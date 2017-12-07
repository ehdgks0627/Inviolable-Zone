using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Collections.Generic;
using HeyRed.Mime;

namespace WALLnutClient
{
    public class DiskManager
    {
        #region [Structure] 디스크 헤더 구조체
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct DISK_HEADER_STRUCTURE
        {
            public fixed byte signature[0x0008];
            public UInt64 blockSize;
            public UInt64 entryFile;
            public UInt64 entryIndex;
            public UInt64 entryBitmap;
            public UInt64 lastFile;
            public UInt64 lastBitmap;
            public UInt64 diskSize;
            public Int64 lastModifyTime;
            public Int64 uuid;
            public fixed byte endSignature[0x0008];
        }
        #endregion

        #region [Structure] 파일 엔트리 구조체
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct ENTRY_FILE_STRUCTURE : ICloneable
        {
            public DiskManager.BLOCKTYPE blockType;
            public UInt64 prevFile;
            public UInt64 nextFile;
            public fixed byte fileName[0x0100];
            public UInt64 offsetData;
            public UInt64 fileSize;
            public UInt64 offsetParent;
            public long timeCreate;
            public long timeModify;
            public fixed byte mimeType[0x0100];
            public fixed byte memo[0x0DC4];

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }
        #endregion

        #region [Structure] 데이터 구조체
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct ENTRY_DATA_STRUCTURE
        {
            public DiskManager.BLOCKTYPE blockType;
            public UInt64 prevFile;
            public UInt64 nextFile;
            public fixed byte data[0x0FEC];
        }
        #endregion

        #region [Structure] 인덱스 폴더 구조체
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FolderIndex
        {
            public string fileName;
            public UInt64 parent;
        }
        #endregion

        #region [Enum] 블록 종류
        public enum BLOCKTYPE : UInt32
        {
            ENTRY_FILE = 1,
            ENTRY_FOLDER,
            DATA,
            BITMAP
        }
        #endregion

        SafeFileHandle handle;
        public bool isActive { get; set; }

        public static string SIGNATURE = "\x00WALLnut";
        public static uint BLOCK_SIZE = 0x1000;
        public static UInt64 BLOCK_END = 0xFAFAFAFAFAFAFAFAL;
        public static UInt64 HEADER_SIZE = 0x0040;
        public static UInt64 STATE_ERROR = 0xFFFFFFFFFFFFFFFFL;

        private UInt64 ENTRY_FILE { get; set; }
        private UInt64 ENTRY_INDEX { get; set; }
        private UInt64 ENTRY_BITMAP { get; set; }

        private UInt64 LAST_FILE { get; set; }
        private UInt64 LAST_BITMAP { get; set; }

        public FileNode root { get; set; }

        #region [Function] [생성자] 헤더의 기본 정보를 읽음
        public unsafe DiskManager(string diskName)
        {
            UInt64 finder, offset;
            handle = DiskIOWrapper.CreateFile(diskName, DiskIOWrapper.GENERIC_READ | DiskIOWrapper.GENERIC_WRITE, DiskIOWrapper.FILE_SHARE_READ, IntPtr.Zero, DiskIOWrapper.OPEN_EXISTING, 0, IntPtr.Zero);
            if (!handle.IsInvalid && !handle.IsClosed)
            {
                byte[] buffer = new byte[BLOCK_SIZE];
                ReadBlock(ref buffer, 0);
                fixed (byte* ptrBuffer = &buffer[0])
                {
                    DISK_HEADER_STRUCTURE* headerPtr = (DISK_HEADER_STRUCTURE*)ptrBuffer;
                    for (int i = 0; i < SIGNATURE.Length; i++)
                    {
                        if (!(headerPtr->signature[i].Equals(Convert.ToByte(SIGNATURE[i])) && headerPtr->endSignature[i].Equals(Convert.ToByte(SIGNATURE[i])))) //err, git diff로 체크
                        {
                            isActive = false;
                            return;
                        }
                    }
                    ENTRY_FILE = headerPtr->entryFile;
                    ENTRY_INDEX = headerPtr->entryIndex;
                    ENTRY_BITMAP = headerPtr->entryBitmap;
                    LAST_FILE = headerPtr->lastFile;
                    LAST_BITMAP = headerPtr->lastBitmap;

                    ENTRY_FILE_STRUCTURE* filePtr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;

                    Dictionary<UInt64, FolderIndex> folderList = new Dictionary<UInt64, FolderIndex>();
                    finder = ENTRY_FILE;
                    while (!finder.Equals(BLOCK_END))
                    {
                        ReadBlock(ref buffer, finder);
                        if (filePtr->blockType.Equals(BLOCKTYPE.ENTRY_FOLDER))
                        {
                            folderList.Add(finder, new FolderIndex { fileName = Marshal.PtrToStringUni((IntPtr)filePtr->fileName), parent = filePtr->offsetParent });
                        }
                        finder = filePtr->nextFile;
                    }
                    finder = ENTRY_FILE;
                    while (!finder.Equals(BLOCK_END))
                    {
                        ReadBlock(ref buffer, finder);
                        FileNode newNode = null;
                        newNode = new FileNode((ENTRY_FILE_STRUCTURE)(filePtr->Clone()), finder, Marshal.PtrToStringUni((IntPtr)filePtr->mimeType));
                        offset = filePtr->offsetParent;
                        if (finder.Equals(ENTRY_FILE))
                        {
                            root = newNode;
                        }
                        else
                        {
                            string rootPath = string.Empty;
                            if (offset.Equals(ENTRY_FILE))
                            {
                                rootPath = "\\";
                            }
                            else
                            {
                                while (!offset.Equals(ENTRY_FILE))
                                {
                                    rootPath = "\\" + folderList[offset].fileName + rootPath;
                                    offset = folderList[offset].parent;
                                }
                            }
                            FileNode parent = root.FindNodeByFilename(rootPath);
                            if (!Object.ReferenceEquals(parent, null))
                            {
                                parent.AppendChild(newNode);
                            }
                        }
                        finder = filePtr->nextFile;
                    }

                    isActive = true;
                    return;
                }
            }
            else
            {
                isActive = false;
                return;
            }
        }
        #endregion

        #region [Function] 디스크 사용량을 구합니다
        public unsafe UInt64 getUsage()
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            UInt64 size = 0;
            UInt64 finder = ENTRY_BITMAP;
            fixed (byte* ptrBuffer = &buffer[0])
            {
                ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                while (!finder.Equals(BLOCK_END))
                {
                    ReadBlock(ref buffer, finder);
                    for (int i = 0; i < 0xFEC; i++)
                    {
                        size += ptr->data[i];
                    }
                    finder = ptr->nextFile;
                }
            }
            return size * BLOCK_SIZE;
        }
        #endregion

        #region [Function] 디스크 헤더의 last_modify_time을 현재 시간으로 업데이트함
        private unsafe Int64 updateTime()
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            Int64 now = DateTime.Now.ToFileTimeUtc();
            fixed (byte* ptrBuffer = &buffer[0])
            {
                DISK_HEADER_STRUCTURE* ptr = (DISK_HEADER_STRUCTURE*)ptrBuffer;
                ReadBlock(ref buffer, 0);
                ptr->lastModifyTime = now;
                WriteBlock(buffer, 0);
            }
            return now;
        }
        #endregion

        #region [Function] byte 배열에 string 유니코드 문자열을 대입
        public unsafe static void ustrcpy(byte* destination, string source)
        {
            int length = source.Length;
            char[] c = new char[1];
            fixed (char* ptr_c = &c[0])
            {
                for (int i = 0; i < length; i++)
                {
                    byte* ptr = (byte*)ptr_c;
                    c[0] = source[i];
                    destination[i * 2] = ptr[0];
                    destination[i * 2 + 1] = ptr[1];
                }
            }
        }
        #endregion

        #region [Function] byte 배열에 string 문자열을 대입
        public unsafe static void strcpy(byte* destination, string source)
        {
            int length = source.Length;
            for (int i = 0; i < length; i++)
            {
                destination[i] = Convert.ToByte(source[i]);
            }
        }
        #endregion

        #region [Function] byte 배열에 byte 배열을 대입
        public unsafe static void bytecpy(byte* destination, byte* source, ulong index, ulong size)
        {
            for (ulong i = 0; i < size; i++)
            {
                destination[index + i] = source[i];
            }
        }
        #endregion

        #region [Function] 비트맵 에서 사용 가능한 오프셋을 찾아 반환
        public unsafe UInt64 NewBitMapBlock()
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            UInt64 finder = ENTRY_BITMAP;
            UInt64 new_offset = 0x0FEC * 8;
            ReadBlock(ref buffer, finder);
            fixed (byte* ptrBuffer = &buffer[0])
            {
                ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                while (!ptr->nextFile.Equals(BLOCK_END))
                {
                    finder = ptr->nextFile;
                    ClearBuffer(ref buffer);
                    ReadBlock(ref buffer, finder);
                    new_offset += 0x0FEC * 8;
                }
                ptr->nextFile = new_offset;
                WriteBlock(buffer, finder);

                ClearBuffer(ref buffer);
                ptr->blockType = BLOCKTYPE.BITMAP;
                ptr->prevFile = finder;
                ptr->nextFile = BLOCK_END;
                WriteBlock(buffer, new_offset);
                SetBitMapBlock(new_offset);
            }
            return new_offset;
        }
        #endregion

        #region [Function] 바이트중 어느 비트가 사용 가능한지 반환
        public byte GetAvailableBit(byte b)
        {
            for (byte i = 0; i < 8; i++)
            {
                if ((b & (1 << i)).Equals(0))
                {
                    return i;
                }
            }
            return 0xFF;
        }
        #endregion

        #region [Function] 블록타입에 맞는 블록중 사용 가능한 블록을 찾고 할당하여 오프셋 반환
        public unsafe List<UInt64> AvailableBlock(BLOCKTYPE blockType, UInt64 size)
        {
            try
            {
                byte[] buffer = new byte[BLOCK_SIZE];
                bool work = true;
                UInt64 finder = 0xFFFFFFFFFFFFFFFFL, newOffset = 0, allocatedBlock = 0;
                List<UInt64> newBlock = new List<UInt64>();
                if (!Enum.IsDefined(typeof(BLOCKTYPE), blockType))
                {
                    return null;
                }
                fixed (byte* ptrBuffer = &buffer[0])
                {

                    ENTRY_FILE_STRUCTURE* filePtr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                    ENTRY_DATA_STRUCTURE* dataPtr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;

                    switch (blockType)
                    {
                        case BLOCKTYPE.ENTRY_FILE:
                        case BLOCKTYPE.ENTRY_FOLDER:
                            finder = ENTRY_BITMAP;
                            ReadBlock(ref buffer, finder);
                            while (!finder.Equals(BLOCK_END) && work)
                            {
                                for (ulong i = 0; i < 0x0FEC && work; i++)
                                {
                                    while (!dataPtr->data[i].Equals(0xFF) && work)
                                    {
                                        byte b = GetAvailableBit(dataPtr->data[i]);
                                        newBlock.Add(newOffset + i * 8 + b);
                                        dataPtr->data[i] |= Convert.ToByte((1 << (int)b));
                                        if (Convert.ToUInt64(newBlock.Count) == size)
                                        {
                                            work = false;
                                            break;
                                        }
                                    }
                                }
                                if (work)
                                {
                                    finder = dataPtr->nextFile;
                                    ReadBlock(ref buffer, finder);
                                    newOffset += 0x0FEC * 8;
                                }

                            }
                            if (work)
                            {
                                NewBitMapBlock();

                                newBlock.AddRange(AvailableBlock(blockType, size - allocatedBlock));
                                return newBlock;
                            }
                            else
                            {
                                foreach (UInt64 offset in newBlock)
                                {
                                    ReadBlock(ref buffer, LAST_FILE);
                                    filePtr->nextFile = offset;
                                    WriteBlock(buffer, LAST_FILE);
                                    SetBitMapBlock(offset);
                                    ClearBuffer(ref buffer);
                                    filePtr->blockType = BLOCKTYPE.ENTRY_FILE;
                                    filePtr->prevFile = LAST_FILE;
                                    filePtr->nextFile = BLOCK_END;
                                    WriteBlock(buffer, offset);
                                    LAST_FILE = offset;
                                }
                                return newBlock;
                            }
                            break;

                        case BLOCKTYPE.DATA:
                            finder = ENTRY_BITMAP;
                            ReadBlock(ref buffer, finder);
                            while (!finder.Equals(BLOCK_END) && work)
                            {
                                for (ulong i = 0; i < 0x0FEC && work; i++)
                                {
                                    while (!dataPtr->data[i].Equals(0xFF) && work)
                                    {
                                        byte b = GetAvailableBit(dataPtr->data[i]);
                                        newBlock.Add(newOffset + i * 8 + b);
                                        dataPtr->data[i] |= Convert.ToByte((1 << (int)b));
                                        if (Convert.ToUInt64(newBlock.Count) == size)
                                        {
                                            work = false;
                                            break;
                                        }
                                    }
                                }
                                if (work)
                                {
                                    finder = dataPtr->nextFile;
                                    ReadBlock(ref buffer, finder);
                                    newOffset += 0x0FEC * 8;
                                }
                            }
                            if (work)
                            {
                                NewBitMapBlock();
                                newBlock.AddRange(AvailableBlock(BLOCKTYPE.DATA, size - allocatedBlock));
                                return newBlock;
                            }
                            else
                            {
                                foreach (UInt64 offset in newBlock)
                                {
                                    SetBitMapBlock(offset);
                                    ClearBuffer(ref buffer);
                                    dataPtr->blockType = BLOCKTYPE.DATA;
                                    dataPtr->prevFile = BLOCK_END;
                                    dataPtr->nextFile = BLOCK_END;
                                    WriteBlock(buffer, offset);
                                }

                                return newBlock;
                            }
                            break;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region [Function] 디스크 offset 번째 블록에 buffer를 Read
        public unsafe void ReadBlock(ref byte[] buffer, UInt64 offset)
        {
            try
            {
                uint[] read = new uint[1];
                offset *= BLOCK_SIZE;
                fixed (uint* ptrRead = &read[0])
                {
                    fixed (byte* ptrBuffer = &buffer[0x0000])
                    {
                        DiskIOWrapper.SetFilePointerEx(handle, offset, out offset, DiskIOWrapper.FILE_BEGIN);
                        DiskIOWrapper.ReadFile(handle, ptrBuffer, BLOCK_SIZE, ptrRead, IntPtr.Zero);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public unsafe static void ReadBlock(ref byte[] buffer, UInt64 offset, string diskName)
        {
            SafeFileHandle handle = DiskIOWrapper.CreateFile(diskName, DiskIOWrapper.GENERIC_READ | DiskIOWrapper.GENERIC_WRITE, DiskIOWrapper.FILE_SHARE_READ, IntPtr.Zero, DiskIOWrapper.OPEN_EXISTING, 0, IntPtr.Zero);
            uint[] read = new uint[1];
            offset *= BLOCK_SIZE;
            fixed (uint* ptrRead = &read[0])
            {
                fixed (byte* ptrBuffer = &buffer[0x0000])
                {
                    DiskIOWrapper.SetFilePointerEx(handle, offset, out offset, DiskIOWrapper.FILE_BEGIN);
                    DiskIOWrapper.ReadFile(handle, ptrBuffer, BLOCK_SIZE, ptrRead, IntPtr.Zero);
                }
            }
            handle.Close();
        }
        #endregion

        #region [Function] 디스크 offset 번째 블록에 buffer를 Write
        public unsafe void WriteBlock(byte[] buffer, UInt64 offset)
        {
            uint[] read = new uint[1];
            offset *= BLOCK_SIZE;
            fixed (uint* ptrRead = &read[0])
            {
                fixed (byte* ptrBuffer = &buffer[0x0000])
                {
                    DiskIOWrapper.SetFilePointerEx(handle, offset, out offset, DiskIOWrapper.FILE_BEGIN);
                    DiskIOWrapper.WriteFile(handle, ptrBuffer, BLOCK_SIZE, ptrRead, IntPtr.Zero);
                }
            }
        }
        #endregion

        #region [Function] 해당 디스크에서 비트맵 블록에서 offset의 비트 Set
        public unsafe bool SetBitMapBlock(UInt64 offset)
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            UInt64 finder = ENTRY_BITMAP;
            UInt64 chunk = offset / 8 / 0x0FEC;
            UInt64 block = (offset - 8 * 0x0FEC * chunk) / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));
            fixed (byte* ptrBuffer = &buffer[0])
            {
                ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                while (!chunk.Equals(UInt64.MaxValue))
                {
                    ReadBlock(ref buffer, finder);
                    chunk--;
                    if (ptr->nextFile.Equals(BLOCK_END) && !chunk.Equals(UInt64.MaxValue))
                    {
                        UInt64 new_block = NewBitMapBlock();

                        if (chunk.Equals(UInt64.MaxValue))
                        {
                            SetBitMapBlock(offset);
                            return true;
                        }
                        finder = new_block;
                    }
                    else if (!chunk.Equals(UInt64.MaxValue))
                    {
                        finder = ptr->nextFile;
                    }
                }

                if ((ptr->data[block] & mask).Equals(0))
                {
                    ptr->data[block] ^= mask;
                    WriteBlock(buffer, finder);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion

        #region [Function] 해당 디스크에서 비트맵 블록에서 offset의 비트 UnSet
        public unsafe bool UnSetBitMapBlock(UInt64 offset)
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            UInt64 finder = ENTRY_BITMAP;
            UInt64 chunk = offset / 8 / 0x0FEC;
            UInt64 block = (offset - 8 * 0x0FEC * chunk) / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));

            fixed (byte* ptrBuffer = &buffer[0])
            {
                ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                while (!chunk.Equals(UInt64.MaxValue))
                {
                    ReadBlock(ref buffer, finder);
                    chunk--;
                    if (!chunk.Equals(UInt64.MaxValue))
                    {
                        finder = ptr->nextFile;
                    }
                    if (finder.Equals(BLOCK_END))
                    {
                        return false;
                    }
                }

                if (!(ptr->data[block] & mask).Equals(0))
                {
                    ptr->data[block] ^= mask;
                    WriteBlock(buffer, finder);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion

        #region [Function] path를 기준으로 파일 엔트리 오프셋을 찾기
        public unsafe UInt64 PathToOffset(string fileName)
        {
            FileNode node;
            if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\\\\"))
            {
                return BLOCK_END;
            }
            node = root.FindNodeByFilename(fileName);
            if (Object.ReferenceEquals(node, null))
            {
                return BLOCK_END;
            }
            else
            {
                return node.index;
            }
        }
        #endregion

        #region [Function] path를 기준으로 파일을 찾기
        public unsafe bool ReadFile(string fileName, out byte[] fileContent)
        {
            UInt64 offset = PathToOffset(fileName);
            UInt64 finder;
            ulong readsize = 0;
            UInt64 filesize;
            byte[] buffer = new byte[BLOCK_SIZE];
            fileContent = null;
            if (!offset.Equals(BLOCK_END))
            {
                ReadBlock(ref buffer, offset);
                fixed (byte* ptrBuffer = &buffer[0])
                {
                    ENTRY_FILE_STRUCTURE* filePtr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                    finder = filePtr->offsetData;
                    filesize = filePtr->fileSize;
                    fileContent = new byte[filesize];
                }
                fixed (byte* ptr_filecontent = &fileContent[0])
                {
                    fixed (byte* ptrBuffer = &buffer[0])
                    {
                        ENTRY_DATA_STRUCTURE* dataPtr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                        while (filesize - readsize > 0)
                        {
                            UInt64 buffer_read_size = (filesize - readsize > 0x0FEC) ? 0x0FEC : filesize - readsize;
                            ReadBlock(ref buffer, finder);
                            bytecpy(ptr_filecontent, dataPtr->data, readsize, buffer_read_size);
                            finder = dataPtr->nextFile;
                            readsize += buffer_read_size;
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public unsafe bool ReadFile(string fileName, string outFileName) //buffered
        {
            throw new NotImplementedException();
            UInt64 offset = PathToOffset(fileName);
            UInt64 finder;
            ulong readsize = 0;
            UInt64 filesize;
            byte[] buffer = new byte[BLOCK_SIZE];
            if (!offset.Equals(BLOCK_END))
            {
                ReadBlock(ref buffer, offset);
                fixed (byte* ptrBuffer = &buffer[0])
                {
                    ENTRY_FILE_STRUCTURE* filePtr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                    finder = filePtr->offsetData;
                    filesize = filePtr->fileSize;
                }
                fixed (byte* ptrBuffer = &buffer[0])
                {
                    ENTRY_DATA_STRUCTURE* dataPtr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                    while (filesize - readsize > 0)
                    {
                        UInt64 buffer_read_size = (filesize - readsize > 0x0FEC) ? 0x0FEC : filesize - readsize;
                        ReadBlock(ref buffer, finder);

                        finder = dataPtr->nextFile;
                        readsize += buffer_read_size;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [Function] 버퍼를 초기화합니다
        public static void ClearBuffer(ref byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }
        }
        #endregion

        #region [Function] path를 기준으로 폴더를 저장 / 덮어쓰기
        public unsafe bool WriteFolder(string fileName)
        {
            UInt64 offset = PathToOffset(fileName);
            byte[] buffer = new byte[BLOCK_SIZE];
            ClearBuffer(ref buffer);
            long now = DateTime.Now.ToFileTimeUtc();
            if (fileName.Equals(@"\"))
            {
                return false;
            }
            fixed (byte* ptrBuffer = &buffer[0])
            {
                ENTRY_FILE_STRUCTURE* filePtr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                UInt64 offset_parent = BLOCK_END;
                FileNode newNode = null, parent;
                parent = root.FindNodeByFilename(fileName.Substring(0, fileName.LastIndexOf('\\') + 1), 0, true);
                offset_parent = (!Object.ReferenceEquals(parent, null)) ? (parent.index) : BLOCK_END;
                String[] check = fileName.Split('\\');
                for (int i = 1; i < check.Length; i++)
                {
                    if (check[i].Equals(string.Empty))
                    {
                        return false;
                    }
                }
                if (offset_parent.Equals(BLOCK_END) || //부모 노드가 없거나
                    !parent.blockType.Equals(BLOCKTYPE.ENTRY_FOLDER)) //부모 노드가 폴더가 아니거나
                {
                    return false;
                }
                if (!offset.Equals(BLOCK_END)) //폴더가 있을경우
                {
                    ReadBlock(ref buffer, offset);
                    if (!filePtr->blockType.Equals(BLOCKTYPE.ENTRY_FOLDER))
                    {
                        return false;
                    }
                    ustrcpy(filePtr->fileName, fileName.Split('\\')[fileName.Split('\\').Length - 1]);
                    filePtr->timeModify = now;
                    WriteBlock(buffer, offset);

                    root.FindNodeByFilename(fileName).UpdateInfo((ENTRY_FILE_STRUCTURE)filePtr->Clone());
                    FileNode.lastUpdate = updateTime();
                    return true;
                }
                else
                {
                    offset = AvailableBlock(BLOCKTYPE.ENTRY_FOLDER, 1)[0];
                    ReadBlock(ref buffer, offset);
                    ustrcpy(filePtr->fileName, fileName.Split('\\')[fileName.Split('\\').Length - 1]);
                    filePtr->fileSize = 0;
                    filePtr->offsetData = BLOCK_END;
                    filePtr->timeCreate = now;
                    filePtr->timeModify = now;
                    filePtr->blockType = BLOCKTYPE.ENTRY_FOLDER;
                    filePtr->offsetParent = offset_parent;
                    WriteBlock(buffer, offset);

                    newNode = new FileNode((ENTRY_FILE_STRUCTURE)(filePtr->Clone()), offset, "");
                    parent = root.FindNodeByFilename(fileName.Substring(0, fileName.LastIndexOf('\\') + 1));
                    parent.AppendChild(newNode);
                    FileNode.lastUpdate = updateTime();
                    return true;
                }
            }
        }
        #endregion

        #region [Function] path를 기준으로 파일을 저장 / 덮어쓰기
        public unsafe bool WriteFile(string fileName, byte[] fileData, string mimeType)
        {
            UInt64 offset;
            UInt64 finder, PREV_LAST_FILE, offsetParent;
            FileNode parent, node;
            byte[] buffer = new byte[BLOCK_SIZE];
            fixed (byte* ptrBuffer = &buffer[0])
            {
                ENTRY_FILE_STRUCTURE* filePtr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                ENTRY_DATA_STRUCTURE* dataPtr = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                node = root.FindNodeByFilename(fileName);
                parent = root.FindNodeByFilename(fileName.Substring(0, fileName.LastIndexOf('\\') + 1), 0, true);
                offset = (!Object.ReferenceEquals(node, null)) ? (node.index) : (BLOCK_END);
                if (!offset.Equals(BLOCK_END))
                {
                    ReadBlock(ref buffer, offset);
                    if (!filePtr->blockType.Equals(BLOCKTYPE.ENTRY_FILE))
                    {
                        return false;
                    }
                    //mimeType = Marshal.PtrToStringUni((IntPtr)filePtr->mimeType);
                    node.DeleteNode(this);
                }
                offsetParent = (!Object.ReferenceEquals(parent, null)) ? (parent.index) : BLOCK_END;
                if (offsetParent.Equals(BLOCK_END))
                {
                    return false;
                }
                PREV_LAST_FILE = LAST_FILE;
                finder = AvailableBlock(BLOCKTYPE.ENTRY_FILE, 1)[0];
                long now = DateTime.Now.ToFileTimeUtc();
                List<UInt64> blockList = new List<UInt64>();
                ClearBuffer(ref buffer);
                ReadBlock(ref buffer, finder);
                filePtr->fileSize = (ulong)fileData.Length;
                blockList = AvailableBlock(BLOCKTYPE.DATA, (filePtr->fileSize / 0x0FEC) + 1);

                ustrcpy(filePtr->fileName, fileName.Split('\\')[fileName.Split('\\').Length - 1]);
                filePtr->timeCreate = now;
                filePtr->timeCreate = now;
                if (filePtr->fileSize.Equals(0))
                {
                    filePtr->offsetData = BLOCK_END;
                }
                else
                {
                    filePtr->offsetData = blockList[0];
                }
                filePtr->prevFile = PREV_LAST_FILE;
                filePtr->offsetParent = offsetParent;
                filePtr->nextFile = BLOCK_END;
                //filePtr->memo = 
                WriteBlock(buffer, finder);

                FileNode newNode = null;
                newNode = new FileNode((ENTRY_FILE_STRUCTURE)(filePtr->Clone()), finder, mimeType);
                parent.AppendChild(newNode);
                FileNode.lastUpdate = updateTime();

                for (int i = 0; i < blockList.Count; i++)
                {
                    ClearBuffer(ref buffer);
                    dataPtr->blockType = BLOCKTYPE.DATA;
                    if (i.Equals(0))
                    {
                        dataPtr->prevFile = BLOCK_END;
                    }
                    else
                    {
                        dataPtr->prevFile = blockList[i - 1];
                    }
                    if (i.Equals(blockList.Count - 1))
                    {
                        dataPtr->nextFile = BLOCK_END;

                        for (uint j = 0; j < fileData.Length - 0xFEC * i; j++)
                        {
                            dataPtr->data[j] = fileData[i * 0x0FEC + j];
                        }
                    }
                    else
                    {
                        dataPtr->nextFile = blockList[i + 1];

                        for (uint j = 0; j < 0x0FEC; j++)
                        {
                            dataPtr->data[j] = fileData[i * 0x0FEC + j];
                        }
                    }
                    WriteBlock(buffer, blockList[i]);
                }
            }
            return true;
        }
        #endregion

        #region [Function] path를 기준으로 이름 변경
        public unsafe bool Rename(string prevName, string newName)
        {
            string prevFileName = prevName.Split('\\')[newName.Split('\\').Length - 1];
            string fileName = newName.Split('\\')[newName.Split('\\').Length - 1];
            FileNode target = root.FindNodeByFilename(prevName);
            UInt64 offset;
            byte[] buffer;
            if (Object.ReferenceEquals(target, null))
            {
                return false; 
            }
            offset = target.index;
            buffer = new byte[BLOCK_SIZE];
            target.Rename(fileName);
            target.root.child.Remove(prevFileName);
            target.root.child.Add(fileName, target);
            fixed (byte* ptrBuffer = &buffer[0])
            {
                ENTRY_FILE_STRUCTURE* filePtr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                ReadBlock(ref buffer, offset);
                ustrcpy(filePtr->fileName, fileName + "\x00");
                WriteBlock(buffer, offset);
            }
            return true;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 삭제
        public unsafe bool DeleteFile(string fileName)
        {
            FileNode node;
            node = root.FindNodeByFilename(fileName);
            if (Object.ReferenceEquals(node, null))
            {
                return false;
            }
            else
            {
                node.DeleteNode(this);
                return true;
            }
        }
        #endregion

        #region [Function] path를 기준으로 파일을 삭제(데이터를 삭제)
        public unsafe bool _DeleteFile(string fileName)
        {
            UInt64 offset = PathToOffset(fileName);
            UInt64 finder;
            UInt64 prevFile, nextFile;
            byte[] buffer = new byte[BLOCK_SIZE];
            if (!offset.Equals(BLOCK_END))
            {
                ReadBlock(ref buffer, offset);
                fixed (byte* ptrBuffer = &buffer[0])
                {
                    ENTRY_FILE_STRUCTURE* ptr = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                    finder = ptr->offsetData;
                    prevFile = ptr->prevFile;
                    nextFile = ptr->nextFile;
                    UnSetBitMapBlock(offset);

                    if (offset.Equals(LAST_FILE))
                    {
                        LAST_FILE = prevFile;
                    }
                    if (!prevFile.Equals(BLOCK_END))
                    {
                        ReadBlock(ref buffer, prevFile);
                        ptr->nextFile = nextFile;
                        WriteBlock(buffer, prevFile);
                    }
                    if (!nextFile.Equals(BLOCK_END))
                    {
                        ReadBlock(ref buffer, nextFile);
                        ptr->prevFile = prevFile;
                        WriteBlock(buffer, nextFile);
                    }
                    while (!finder.Equals(BLOCK_END))
                    {
                        ReadBlock(ref buffer, finder);
                        UnSetBitMapBlock(finder);
                        finder = ptr->nextFile;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [Function] 비트맵 블록에서 해당 오프셋의 비트를 Set
        private unsafe static bool SetBit(byte* data, UInt64 offset)
        {
            UInt64 block = offset / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));
            if ((data[block] & mask).Equals(0))
            {
                data[block] |= mask;
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [Function] 비트맵 블록에서 해당 오프셋의 비트를 UnSet
        private unsafe static bool UnSetBit(byte* data, UInt64 offset)
        {
            UInt64 block = offset / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));
            if (!(data[block] & mask).Equals(0))
            {
                data[block] ^= mask;
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [Funciton] 디스크를 인자로 받아 포맷합니다
        public static Int64 FormatDisk(DiskInfo diskInfo)
        {
            Random r = new Random();
            Int64 uuid;
            unsafe
            {
                SafeFileHandle handle = DiskIOWrapper.CreateFile(diskInfo.deviceID, DiskIOWrapper.GENERIC_READ | DiskIOWrapper.GENERIC_WRITE, DiskIOWrapper.FILE_SHARE_READ, IntPtr.Zero, DiskIOWrapper.OPEN_EXISTING, 0, IntPtr.Zero);
                if (handle.IsInvalid)
                {
                    MessageBox.Show("관리자 권한이 필요합니다", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return 0x0;
                }
                byte[] buffer = new byte[BLOCK_SIZE];
                uint[] read = new uint[1];
                fixed (byte* ptrBuffer = &buffer[0])
                {
                    fixed (uint* ptrRead = &read[0])
                    {
                        ulong offset = 0;

                        DiskIOWrapper.SetFilePointerEx(handle, offset, out offset, DiskIOWrapper.FILE_BEGIN);
                        DiskIOWrapper.ReadFile(handle, ptrBuffer, BLOCK_SIZE, ptrRead, IntPtr.Zero);

                        BinaryWriter bw = new BinaryWriter(File.Open(diskInfo.deviceID.Replace("\\", "_") + ".backup", FileMode.Create));
                        foreach (byte b in buffer) { bw.Write(b); }
                        bw.Close();

                        // 헤더 생성
                        ClearBuffer(ref buffer);
                        DISK_HEADER_STRUCTURE* header = (DISK_HEADER_STRUCTURE*)ptrBuffer;
                        strcpy(header->signature, SIGNATURE);
                        header->blockSize = BLOCK_SIZE;
                        header->entryFile = 2;
                        header->entryIndex = 0xFFFFFFFFFFFFFFFFL;
                        header->entryBitmap = 1;
                        header->lastFile = 2;
                        header->lastBitmap = 1;
                        header->diskSize = diskInfo.size;
                        do
                        {
                            uuid = (Convert.ToInt64(r.Next(0, int.MaxValue)) << 32);
                            uuid |= Convert.ToInt64(r.Next(0, int.MaxValue));
                        } while (uuid == 0);
                        header->uuid = uuid;
                        strcpy(header->endSignature, SIGNATURE);
                        DiskIOWrapper.SetFilePointerEx(handle, offset, out offset, DiskIOWrapper.FILE_BEGIN);
                        DiskIOWrapper.WriteFile(handle, ptrBuffer, BLOCK_SIZE, ptrRead, IntPtr.Zero);

                        // 초기 비트맵 생성
                        ClearBuffer(ref buffer);
                        ENTRY_DATA_STRUCTURE* bitmap = (ENTRY_DATA_STRUCTURE*)ptrBuffer;
                        bitmap->blockType = BLOCKTYPE.BITMAP;
                        bitmap->prevFile = BLOCK_END;
                        bitmap->nextFile = BLOCK_END;
                        SetBit(bitmap->data, 0x0000); // HEADER
                        SetBit(bitmap->data, 0x0001); // BITMAP BLOCK
                        SetBit(bitmap->data, 0x0002); // FILE BLOCK

                        offset = 1 * BLOCK_SIZE;
                        DiskIOWrapper.SetFilePointerEx(handle, offset, out offset, DiskIOWrapper.FILE_BEGIN);
                        DiskIOWrapper.WriteFile(handle, ptrBuffer, BLOCK_SIZE, ptrRead, IntPtr.Zero);

                        // 초기 파일 엔트리 생성
                        ClearBuffer(ref buffer);
                        long now = DateTime.Now.ToFileTimeUtc();

                        ENTRY_FILE_STRUCTURE* file = (ENTRY_FILE_STRUCTURE*)ptrBuffer;
                        file->blockType = BLOCKTYPE.ENTRY_FOLDER;
                        file->prevFile = BLOCK_END;
                        file->nextFile = BLOCK_END;
                        file->offsetData = BLOCK_END;
                        file->fileSize = 0;
                        file->offsetParent = BLOCK_END;
                        file->timeCreate = now;
                        file->timeModify = now;

                        offset = 2 * BLOCK_SIZE;
                        DiskIOWrapper.SetFilePointerEx(handle, offset, out offset, DiskIOWrapper.FILE_BEGIN);
                        DiskIOWrapper.WriteFile(handle, ptrBuffer, BLOCK_SIZE, ptrRead, IntPtr.Zero);
                    }
                }
                handle.Close();
            }
            return uuid;
        }
        #endregion

        #region [Function] [소멸자] 
        ~DiskManager()
        {
            if (isActive)
            {
                isActive = false;
                handle.Close();
            }
        }
        #endregion
    }
}