using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace WALLnutClient
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct DISK_HEADER_STRUCTURE
    {
        public fixed byte signature[0x0008];
        public UInt64 block_size;
        public UInt64 entry_file;
        public UInt64 entry_index;
        public UInt64 entry_bitmap;
        public UInt64 last_file;
        public UInt64 last_bitmap;
        public UInt64 disk_size;
        public fixed byte reserved[0x0008];
        public fixed byte end_signature[0x0008];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct ENTRY_FILE_STRUCTURE
    {
        public DiskManager.BLOCKTYPE type;
        public UInt64 prev_file;
        public UInt64 next_file;
        public fixed byte filename[0x0100];
        public UInt64 offset_data;
        public UInt64 filesize;
        public UInt64 offset_parent;
        public long time_create;
        public long time_modify;
        public fixed byte memo[0x0EC4];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct ENTRY_DATA_STRUCTURE
    {
        public DiskManager.BLOCKTYPE type;
        public UInt64 prev_file;
        public UInt64 next_file;
        public fixed byte data[0x0FEC];
    }

    public class DiskManager
    {
        SafeFileHandle handle;
        public bool isActive { get; set; }

        public static string SIGNATURE = "WALLnut\x00";
        public static uint BLOCK_SIZE = 0x1000;
        public static UInt64 BLOCK_END = 0xFAFAFAFAFAFAFAFAL;
        public static UInt64 HEADER_SIZE = 0x0040;
        public static UInt64 STATE_ERROR = 0xFFFFFFFFFFFFFFFFL;

        private UInt64 ENTRY_FILE { get; set; }
        private UInt64 ENTRY_INDEX { get; set; }
        private UInt64 ENTRY_BITMAP { get; set; }

        private UInt64 LAST_FILE { get; set; }
        private UInt64 LAST_BITMAP { get; set; }

        public enum BLOCKTYPE : UInt32
        {
            ENTRY_FILE = 1,
            ENTRY_FOLDER,
            DATA,
            BITMAP
        }

        #region [Function] 생성자, 헤더의 기본 정보를 읽음
        public unsafe DiskManager(string diskname)
        {
            handle = DiskIO.CreateFile(diskname, DiskIO.GENERIC_READ | DiskIO.GENERIC_WRITE, DiskIO.FILE_SHARE_READ | DiskIO.FILE_SHARE_WRITE, IntPtr.Zero, DiskIO.OPEN_EXISTING, 0, IntPtr.Zero);
            if (!handle.IsInvalid && !handle.IsClosed)
            {
                byte[] buffer = new byte[BLOCK_SIZE];
                ReadBlock(ref buffer, 0);
                fixed (byte* ptr_buffer = &buffer[0])
                {
                    DISK_HEADER_STRUCTURE* ptr = (DISK_HEADER_STRUCTURE*)ptr_buffer;
                    for (int i = 0; i < SIGNATURE.Length; i++)
                    {
                        if (ptr->signature[i] != SIGNATURE[i] || ptr->end_signature[i] != SIGNATURE[i])
                        {
                            isActive = false;
                            return;
                        }
                    }
                    ENTRY_FILE = ptr->entry_file;
                    ENTRY_INDEX = ptr->entry_index;
                    ENTRY_BITMAP = ptr->entry_bitmap;
                    LAST_FILE = ptr->last_file;
                    LAST_BITMAP = ptr->last_bitmap;
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
            fixed (byte* ptr_buffer = &buffer[0])
            {
                ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                while (ptr->next_file != BLOCK_END)
                {
                    finder = ptr->next_file;
                    ReadBlock(ref buffer, finder);
                    new_offset += 0x0FEC * 8;
                }
                ptr->next_file = new_offset;
                WriteBlock(buffer, finder);

                ClearBuffer(ref buffer);
                ptr->type = BLOCKTYPE.BITMAP;
                ptr->prev_file = finder;
                ptr->next_file = BLOCK_END;
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
                if ((b & (1 << i)) == 0)
                {
                    return i;
                }
            }
            return 0xFF;
        }
        #endregion

        #region [Function] 블록타입에 맞는 블록중 사용 가능한 블록을 찾고 할당하여 오프셋 반환
        public unsafe UInt64 AvailableBlock(BLOCKTYPE type)
        {
            try
            {
                byte[] buffer = new byte[BLOCK_SIZE];
                bool work = true;
                UInt64 finder = 0xFFFFFFFFFFFFFFFFL, new_block = 0;
                if (!Enum.IsDefined(typeof(BLOCKTYPE), type))
                {
                    return STATE_ERROR;
                }
                fixed (byte* ptr_buffer = &buffer[0])
                {

                    ENTRY_FILE_STRUCTURE* file_ptr = (ENTRY_FILE_STRUCTURE*)ptr_buffer;
                    ENTRY_DATA_STRUCTURE* data_ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;

                    switch (type)
                    {
                        case BLOCKTYPE.ENTRY_FILE:
                            finder = ENTRY_BITMAP;
                            ReadBlock(ref buffer, finder);
                            while (finder != BLOCK_END && work)
                            {
                                for (ulong i = 0; i < 0x0FEC; i++)
                                {
                                    if (data_ptr->data[i] != 0xFF)
                                    {
                                        new_block += i * 8 + GetAvailableBit(data_ptr->data[i]);
                                        work = false;
                                        break;
                                    }
                                }
                                if (work)
                                {
                                    finder = data_ptr->next_file;
                                    ReadBlock(ref buffer, finder);
                                    new_block += 0x0FEC * 8;
                                }
                            }
                            if (work)
                            {
                                NewBitMapBlock();
                                return AvailableBlock(BLOCKTYPE.ENTRY_FILE);
                            }
                            else
                            {
                                ReadBlock(ref buffer, LAST_FILE);
                                file_ptr->next_file = new_block;
                                WriteBlock(buffer, LAST_FILE);
                                SetBitMapBlock(new_block);
                                ClearBuffer(ref buffer);
                                file_ptr->type = BLOCKTYPE.ENTRY_FILE;
                                file_ptr->prev_file = LAST_FILE;
                                file_ptr->next_file = BLOCK_END;
                                WriteBlock(buffer, new_block);
                                LAST_FILE = new_block;
                                return new_block;
                            }
                            break;

                        case BLOCKTYPE.DATA:
                            finder = ENTRY_BITMAP;
                            ReadBlock(ref buffer, finder);
                            while (finder != BLOCK_END && work)
                            {
                                for (ulong i = 0; i < 0x0FEC; i++)
                                {
                                    if (data_ptr->data[i] != 0xFF)
                                    {
                                        new_block += i * 8 + GetAvailableBit(data_ptr->data[i]);
                                        work = false;
                                        break;
                                    }
                                }
                                if (work)
                                {
                                    finder = data_ptr->next_file;
                                    ReadBlock(ref buffer, finder);
                                    new_block += 0x0FEC * 8;
                                }
                            }
                            if (work)
                            {
                                NewBitMapBlock();
                                return AvailableBlock(BLOCKTYPE.DATA);
                            }
                            else
                            {
                                SetBitMapBlock(new_block);
                                ClearBuffer(ref buffer);
                                data_ptr->type = BLOCKTYPE.DATA;
                                data_ptr->prev_file = BLOCK_END;
                                data_ptr->next_file = BLOCK_END;
                                WriteBlock(buffer, new_block);
                                return new_block;
                            }
                            break;
                    }
                }
                return BLOCK_END;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        #endregion

        #region [Function] 디스크 offset 번째 블록에 buffer를 Read
        public unsafe void ReadBlock(ref byte[] buffer, UInt64 offset)
        {
            uint[] read = new uint[1];
            offset *= BLOCK_SIZE;
            fixed (uint* ptr_read = &read[0])
            {
                fixed (byte* ptr_buffer = &buffer[0x0000])
                {
                    DiskIO.SetFilePointerEx(handle, offset, out offset, DiskIO.FILE_BEGIN);
                    DiskIO.ReadFile(handle, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);
                }
            }
        }
        #endregion

        #region [Function] 디스크 offset 번째 블록에 buffer를 Write
        public unsafe void WriteBlock(byte[] buffer, UInt64 offset)
        {
            uint[] read = new uint[1];
            offset *= BLOCK_SIZE;
            fixed (uint* ptr_read = &read[0])
            {
                fixed (byte* ptr_buffer = &buffer[0x0000])
                {
                    DiskIO.SetFilePointerEx(handle, offset, out offset, DiskIO.FILE_BEGIN);
                    DiskIO.WriteFile(handle, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);
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

            fixed (byte* ptr_buffer = &buffer[0])
            {
                ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                while (chunk != UInt64.MaxValue)
                {
                    ReadBlock(ref buffer, finder);
                    chunk--;
                    if (ptr->next_file == BLOCK_END && chunk != UInt64.MaxValue)
                    {
                        UInt64 new_block = NewBitMapBlock();

                        if (chunk == UInt64.MaxValue)
                        {
                            SetBitMapBlock(offset);
                            return true;
                        }
                        finder = new_block;
                    }
                    else if (chunk != UInt64.MaxValue)
                    {
                        finder = ptr->next_file;
                    }
                }


                if ((ptr->data[block] & mask) == 0)
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

            throw new NotImplementedException();
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

            fixed (byte* ptr_buffer = &buffer[0])
            {
                ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                while (chunk != UInt64.MaxValue)
                {
                    ReadBlock(ref buffer, finder);
                    chunk--;
                    if (chunk != UInt64.MaxValue)
                    {
                        finder = ptr->next_file;
                    }
                    if (finder == BLOCK_END)
                    {
                        return false;
                    }
                }

                if ((ptr->data[block] & mask) != 0)
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

            throw new NotImplementedException();
        }
        #endregion

        #region [Function] path를 기준으로 파일 엔트리 오프셋을 찾기
        public unsafe UInt64 Path2Offset(string filename)
        {
            UInt64 finder = ENTRY_FILE;
            UInt64 prev_finder = BLOCK_END;
            byte[] buffer = new byte[BLOCK_SIZE];
            string[] paths = filename.Split('\\');
            if (!paths[0].Equals(""))
            {
                return BLOCK_END;
            }
            for (int i = 1; i < paths.Length; i++)
            {
                while (finder != BLOCK_END)
                {
                    ReadBlock(ref buffer, finder);
                    fixed (byte* ptr = &buffer[0])
                    {
                        ENTRY_FILE_STRUCTURE* file = (ENTRY_FILE_STRUCTURE*)ptr;
                        string compare_filename = Marshal.PtrToStringUni((IntPtr)file->filename); //주의
                        if (paths[i].Equals(compare_filename) && file->offset_parent == prev_finder)
                        {
                            if (i == paths.Length - 1)
                            {
                                return finder;
                            }
                            else
                            {
                                finder = ENTRY_FILE;
                                break;
                            }
                        }
                        prev_finder = finder;
                        finder = file->next_file;
                    }
                }
            }
            return BLOCK_END;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 찾기
        public unsafe bool ReadFile(string filename, out byte[] filecontent)
        {
            UInt64 offset = Path2Offset(filename);
            UInt64 finder;
            ulong readsize = 0;
            UInt64 filesize;
            byte[] buffer = new byte[BLOCK_SIZE];
            filecontent = null;
            if (offset != BLOCK_END)
            {
                ReadBlock(ref buffer, offset);
                fixed (byte* ptr_buffer = &buffer[0])
                {
                    ENTRY_FILE_STRUCTURE* ptr = (ENTRY_FILE_STRUCTURE*)ptr_buffer;
                    finder = ptr->offset_data;
                    filesize = ptr->filesize;
                    filecontent = new byte[filesize];
                }
                fixed (byte* ptr_filecontent = &filecontent[0])
                {
                    fixed (byte* ptr_buffer = &buffer[0])
                    {
                        ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                        while (filesize - readsize > 0)
                        {
                            UInt64 buffer_read_size = (filesize - readsize > 0x0FEC) ? 0x0FEC : filesize - readsize;
                            ReadBlock(ref buffer, finder);
                            bytecpy(ptr_filecontent, ptr->data, readsize, buffer_read_size);
                            finder = ptr->next_file;
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
        #endregion

        #region [Function] 버퍼를 초기화합니다
        public static void ClearBuffer(ref byte[] buffer)
        {
            for(int i=0; i<buffer.Length; i++)
            {
                buffer[i] = 0;
            }
        }
        #endregion

        #region [Function] path를 기준으로 폴더를 저장 / 덮어쓰기
        public unsafe bool WriteFolder(string filename)
        {
            UInt64 offset = Path2Offset(filename);
            byte[] buffer = new byte[BLOCK_SIZE];
            ClearBuffer(ref buffer);
            long now = DateTime.Now.ToFileTimeUtc();
            fixed (byte *ptr_buffer = &buffer[0])
            {
                ENTRY_FILE_STRUCTURE* file_ptr = (ENTRY_FILE_STRUCTURE*)ptr_buffer;
                if (offset != BLOCK_END) //폴더가 있을경우
                {
                    ReadBlock(ref buffer, offset);
                    ustrcpy(file_ptr->filename,filename.Split('\\')[filename.Split('\\').Length - 1]);
                    file_ptr->time_modify = now;
                    WriteBlock(buffer, offset);
                    return true;
                }
                else
                {
                    offset = AvailableBlock(BLOCKTYPE.ENTRY_FILE);
                    ustrcpy(file_ptr->filename, filename.Split('\\')[filename.Split('\\').Length - 1]);
                    file_ptr->filesize = 0;
                    file_ptr->offset_data = BLOCK_END;
                    file_ptr->time_create = now;
                    file_ptr->time_modify = now;
                    file_ptr->type = BLOCKTYPE.ENTRY_FOLDER;
                    file_ptr->offset_parent = Path2Offset(filename.Substring(0, filename.LastIndexOf('\\') + 1));
                    WriteBlock(buffer, offset);
                    return true;
                }
            }
        }
        #endregion

        #region [Function] path를 기준으로 파일을 저장 / 덮어쓰기
        public unsafe bool WriteFile(string filename, string targetfile)
        {
            UInt64 offset = Path2Offset(filename);
            UInt64 finder, prev_LAST_FILE;
            byte[] buffer = new byte[BLOCK_SIZE];
            fixed (byte* ptr_buffer = &buffer[0])
            {
                if (offset != BLOCK_END)
                {
                    DeleteFile(filename);
                }
                ENTRY_FILE_STRUCTURE* file_ptr = (ENTRY_FILE_STRUCTURE*)ptr_buffer;
                ENTRY_DATA_STRUCTURE* data_ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                prev_LAST_FILE = LAST_FILE;
                finder = AvailableBlock(BLOCKTYPE.ENTRY_FILE);
                byte[] data = File.ReadAllBytes(targetfile);
                long now = DateTime.Now.ToFileTimeUtc();
                List<UInt64> block_list = new List<UInt64>();
                ClearBuffer(ref buffer);
                file_ptr->filesize = (ulong)data.Length;
                for (uint i = 0; i <= (file_ptr->filesize / 0x0FEC); i++)
                {
                    block_list.Add(AvailableBlock(BLOCKTYPE.DATA));
                }

                ustrcpy(file_ptr->filename, filename.Split('\\')[filename.Split('\\').Length - 1]);
                file_ptr->time_create = now;
                file_ptr->time_create = now;
                if (file_ptr->filesize == 0)
                {
                    file_ptr->offset_data = BLOCK_END;
                }
                else
                {
                    file_ptr->offset_data = block_list[0];
                }
                file_ptr->prev_file = prev_LAST_FILE;
                file_ptr->offset_parent = Path2Offset(filename.Substring(0, filename.LastIndexOf('\\') + 1));
                file_ptr->next_file = BLOCK_END;
                //file_ptr->memo = 
                WriteBlock(buffer, finder);

                for (int i = 0; i < block_list.Count; i++)
                {
                    data_ptr->type = BLOCKTYPE.DATA;
                    ClearBuffer(ref buffer);
                    if (i == 0)
                    {
                        data_ptr->prev_file = BLOCK_END;
                    }
                    else
                    {
                        data_ptr->prev_file = block_list[i - 1];
                    }
                    if (i == block_list.Count - 1)
                    {
                        data_ptr->next_file = BLOCK_END;

                        for (uint j = 0; j < data.Length - 0xFEC * i; j++)
                        {
                            data_ptr->data[j] = data[i * 0x0FEC + j];
                        }
                    }
                    else
                    {
                        data_ptr->next_file = block_list[i + 1];

                        for (uint j = 0; j < 0x0FEC; j++)
                        {
                            data_ptr->data[j] = data[i * 0x0FEC + j];
                        }
                    }
                    WriteBlock(buffer, block_list[i]);
                }
            }
            return true;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 삭제
        public unsafe bool DeleteFile(string filename)
        {
            UInt64 offset = Path2Offset(filename);
            UInt64 finder;
            UInt64 prev_file, next_file;
            byte[] buffer = new byte[BLOCK_SIZE];
            if (offset != BLOCK_END)
            {
                ReadBlock(ref buffer, offset);
                fixed (byte* ptr_buffer = &buffer[0])
                {
                    ENTRY_FILE_STRUCTURE* ptr = (ENTRY_FILE_STRUCTURE*)ptr_buffer;
                    finder = ptr->offset_data;
                    prev_file = ptr->prev_file;
                    next_file = ptr->next_file;
                    UnSetBitMapBlock(offset);

                    if (offset == LAST_FILE)
                    {
                        LAST_FILE = prev_file;
                    }

                    if (prev_file != BLOCK_END)
                    {
                        ReadBlock(ref buffer, prev_file);
                        ptr->next_file = next_file;
                        WriteBlock(buffer, prev_file);
                    }
                    if (next_file != BLOCK_END)
                    {
                        ReadBlock(ref buffer, next_file);
                        ptr->prev_file = prev_file;
                        WriteBlock(buffer, next_file);
                    }
                    while (finder != BLOCK_END)
                    {
                        ReadBlock(ref buffer, finder);
                        UnSetBitMapBlock(finder);
                        finder = ptr->next_file;
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
            if ((data[block] & mask) == 0)
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
            if ((data[block] & mask) != 0)
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
        public static bool FormatDisk(DiskInfo diskinfo)
        {
            unsafe
            {
                SafeFileHandle h = DiskIO.CreateFile(diskinfo.DeviceID, DiskIO.GENERIC_READ | DiskIO.GENERIC_WRITE, DiskIO.FILE_SHARE_READ | DiskIO.FILE_SHARE_WRITE, IntPtr.Zero, DiskIO.OPEN_EXISTING, 0, IntPtr.Zero);
                if (h.IsInvalid)
                {
                    MessageBox.Show("관리자 권한이 필요합니다", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                byte[] buffer = new byte[BLOCK_SIZE];
                uint[] read = new uint[1];
                fixed (byte* ptr_buffer = &buffer[0])
                {
                    fixed (uint* ptr_read = &read[0])
                    {
                        ulong offset = 0;

                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.ReadFile(h, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);

                        BinaryWriter bw = new BinaryWriter(File.Open(diskinfo.DeviceID.Replace("\\", "_") + ".backup", FileMode.Create));
                        foreach (byte b in buffer) { bw.Write(b); }
                        bw.Close();

                        // 헤더 생성
                        ClearBuffer(ref buffer);
                        DISK_HEADER_STRUCTURE* header = (DISK_HEADER_STRUCTURE*)ptr_buffer;
                        strcpy(header->signature, SIGNATURE);
                        header->block_size = BLOCK_SIZE;
                        header->entry_file = 2;
                        header->entry_index = 0xFFFFFFFFFFFFFFFFL;
                        header->entry_bitmap = 1;
                        header->last_file = 2;
                        header->last_bitmap = 1;
                        header->disk_size = diskinfo.Size;
                        //header->reserved;
                        strcpy(header->end_signature, SIGNATURE);
                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.WriteFile(h, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);

                        // 초기 비트맵 생성
                        ClearBuffer(ref buffer);
                        ENTRY_DATA_STRUCTURE* bitmap = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                        bitmap->type = BLOCKTYPE.BITMAP;
                        bitmap->prev_file = BLOCK_END;
                        bitmap->next_file = BLOCK_END;
                        SetBit(bitmap->data, 0x0000); // HEADER
                        SetBit(bitmap->data, 0x0001); // BITMAP BLOCK
                        SetBit(bitmap->data, 0x0002); // FILE BLOCK

                        offset = 1 * BLOCK_SIZE;
                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.WriteFile(h, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);

                        // 초기 파일 엔트리 생성
                        ClearBuffer(ref buffer);
                        long now = DateTime.Now.ToFileTimeUtc();

                        ENTRY_FILE_STRUCTURE* file = (ENTRY_FILE_STRUCTURE*)ptr_buffer;
                        file->type = BLOCKTYPE.ENTRY_FOLDER;
                        file->prev_file = BLOCK_END;
                        file->next_file = BLOCK_END;
                        //strcpy(file->filename, "\\");
                        file->offset_data = BLOCK_END;
                        file->filesize = 0;
                        file->offset_parent = BLOCK_END;
                        file->time_create = now;
                        file->time_modify = now;

                        offset = 2 * BLOCK_SIZE;
                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.WriteFile(h, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);
                    }
                }
                DiskIO.CloseHandle(h);
            }
            return true;
        }
        #endregion

        ~DiskManager()
        {
            DiskIO.CloseHandle(handle);
        }
    }
}
