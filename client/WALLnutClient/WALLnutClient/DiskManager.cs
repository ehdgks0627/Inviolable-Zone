using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace WALLnutClient
{
    public unsafe struct DISK_HEADER_STRUCTURE
    {
        public fixed byte signature[0x0008];
        public UInt64 block_size;
        public UInt64 entry_file;
        public UInt64 entry_index;
        public UInt64 entry_bitmap;
        public UInt64 disk_size;
        public fixed byte reserved[0x0008];
        public fixed byte end_signature[0x0008];
    }

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

        public enum BLOCKTYPE : UInt32
        {
            ENTRY_FILE,
            ENTRY_FOLDER,
            DATA,
            BITMAP
        }

        public unsafe static void strcpy(byte* destination, string source)
        {
            int length = source.Length;
            for (int i = 0; i < length; i++)
            {
                destination[i] = Convert.ToByte(source[i]);
            }
        }

        public unsafe DiskManager(string diskname)
        {
            handle = DiskIO.CreateFile(diskname, DiskIO.GENERIC_READ | DiskIO.GENERIC_WRITE, DiskIO.FILE_SHARE_READ | DiskIO.FILE_SHARE_WRITE, IntPtr.Zero, DiskIO.OPEN_EXISTING, 0, IntPtr.Zero);
            if (!handle.IsInvalid && !handle.IsClosed)
            {
                byte[] buffer = new byte[BLOCK_SIZE];
                ReadBlock(ref buffer, 0);
                for (int i = 0; i < 0x0008; i++)
                {
                    if (buffer[0x0000 + i] != SIGNATURE[i] || buffer[0x0038 + i] != SIGNATURE[i])
                    {
                        isActive = false;
                        return;
                    }
                }
                // 파일 엔트리 (기본 오프셋 0xFF)
                fixed (byte* ptr = &buffer[0x0010]) { ENTRY_FILE = *(UInt64*)ptr; }
                // 색인 엔트리 (기본 오프셋 0xFF)
                fixed (byte* ptr = &buffer[0x0018]) { ENTRY_INDEX = *(UInt64*)ptr; }
                // 비트맵 엔트리 (기본 오프셋 1)
                fixed (byte* ptr = &buffer[0x0020]) { ENTRY_BITMAP = *(UInt64*)ptr; }
                isActive = true;
                return;
            }
            else
            {
                isActive = false;
                return;
            }
        }


        public UInt64 InitBlock(BLOCKTYPE type)
        {
            if (!Enum.IsDefined(typeof(BLOCKTYPE), type))
            {
                return STATE_ERROR;
            }
            switch (type)
            {
                case BLOCKTYPE.ENTRY_FILE:

                    break;
                case BLOCKTYPE.ENTRY_FOLDER:

                    break;
                case BLOCKTYPE.DATA:

                    break;
                case BLOCKTYPE.BITMAP:

                    break;
            }
            throw new NotImplementedException();
            return 0xFFFFFFFFFFFFFFFFL;
        }

        #region [Function] 디스크 offset 번째 블록에 buffer를 Read
        public unsafe void ReadBlock(ref byte[] buffer, UInt64 offset)
        {
            uint[] read = new uint[1];
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
            UInt64 chunk = offset / 8 / 4096;
            UInt64 block = (offset - 8 * 4096 * chunk) / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));

            while (chunk > 0)
            {
                ReadBlock(ref buffer, finder);
                fixed (byte* ptr_buffer = &buffer[0])
                {
                    chunk--;
                    ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                    if (finder == BLOCK_END)
                    {
                        UInt64 new_block = InitBlock(BLOCKTYPE.BITMAP);
                        ptr->next_file = new_block;
                        WriteBlock(buffer, finder);

                        for (uint i = 0; i < 0x1000; i++) { buffer[i] = 0x00; }
                        ptr->prev_file = finder;
                        ptr->next_file = BLOCK_END;
                        WriteBlock(buffer, new_block);
                    }
                    if (chunk != 0)
                    {
                        finder = ptr->next_file;
                    }
                }

            }
            if ((buffer[block] & mask) == 0)
            {
                buffer[block] ^= mask;
                WriteBlock(buffer, finder);
                return true;
            }
            else
            {
                return false;
            }

            throw new NotImplementedException();
        }
        #endregion

        #region [Function] 해당 디스크에서 비트맵 블록에서 offset의 비트 UnSet
        public unsafe bool UnSetBitMapBlock(UInt64 offset)
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            UInt64 finder = ENTRY_BITMAP;
            UInt64 chunk = offset / 8 / 4096;
            UInt64 block = (offset - 8 * 4096 * chunk) / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));

            while (chunk > 0)
            {
                ReadBlock(ref buffer, finder);
                fixed (byte* ptr_buffer = &buffer[0])
                {
                    chunk--;
                    ENTRY_DATA_STRUCTURE* ptr = (ENTRY_DATA_STRUCTURE*)ptr_buffer;
                    if(chunk != 0)
                    {
                        finder = ptr->next_file;
                    }
                    if (finder == BLOCK_END)
                    {
                        return false;
                    }
                }
                
            }
            if((buffer[block] & mask ) == 1)
            {
                buffer[block] ^= mask;
                WriteBlock(buffer, finder);
                return true;
            }
            else
            {
                return false;
            }

            throw new NotImplementedException();
        }
        #endregion

        #region [Function] path를 기준으로 파일 엔트리 오프셋을 찾기
        public unsafe UInt64 Path2Offset(string filename)
        {
            UInt64 finder = ENTRY_FILE;
            byte[] buffer = new byte[BLOCK_SIZE];
            while (finder != BLOCK_END)
            {
                ReadBlock(ref buffer, finder);
                fixed (byte* ptr = &buffer[0])
                {
                    ENTRY_FILE_STRUCTURE* file = (ENTRY_FILE_STRUCTURE*)ptr;
                    string compare_filename = Marshal.PtrToStringAuto((IntPtr)file->filename); //주의
                    if (filename.Equals(compare_filename))
                    {
                        return finder;
                    }
                    finder = file->next_file;
                }
            }
            return BLOCK_END;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 찾기, Offset 반환
        public UInt64 ReadFile(string filename, ref byte[] filecontent)
        {
            UInt64 offset = Path2Offset(filename);
            if(offset != BLOCK_END)
            {

            }
            else
            {
                
            }
            /*
            Path2Offset으로 파일 유무 확인
            if(있으면)
            {
                데이터 블록 오프셋 얻어서
                바이트 버퍼 생성
                filecontent에 저장
            }
            else(없으면)
            {
                에러
            }
            */
            throw new NotImplementedException();
            return 0xFFFFFFFFFFFFFFFFL;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 저장 / 덮어쓰기
        public UInt64 WriteFile(string filename, string targetfile)
        {
            /*
            Path2Offset으로 파일 유무 확인
            if(있으면)
            {
                기존꺼의 data블록을 찾아 덮어쓰기
                마지막 수정 시간 수정
            } 
            else(없으면)
            {
                새로운 파일 엔트리 블록 할당받고
                엔트리블록에 정보를 쓴 후
                데이터블록을 할당 받고 거따가 쓰기
            }
            */
            throw new NotImplementedException();
            return 0xFFFFFFFFFFFFFFFFL;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 삭제
        public bool DeleteFile(string filename)
        {
            /*
            Path2Offset으로 파일 유무 확인
            if(있으면)
            {
                엔트리 블록 삭제 및 전후 연결
                데이터 블록 삭제
            }
            else(없으면)
            {
                에러
            }
            */
            return false;
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
                        for (uint i = 0; i < 0x1000; i++) { buffer[i] = 0x00; }
                        DISK_HEADER_STRUCTURE* header = (DISK_HEADER_STRUCTURE*)ptr_buffer;
                        strcpy(header->signature, SIGNATURE);
                        header->block_size = BLOCK_SIZE;
                        header->entry_file = 2;
                        header->entry_index = 0xFFFFFFFFFFFFFFFFL;
                        header->entry_bitmap = 1;
                        header->disk_size = diskinfo.Size;
                        //header->reserved;
                        strcpy(header->end_signature, SIGNATURE);
                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.WriteFile(h, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);

                        // 초기 비트맵 생성
                        for (uint i = 0; i < BLOCK_SIZE; i++) { buffer[i] = 0x00; }
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
                        for (uint i = 0; i < BLOCK_SIZE; i++) { buffer[i] = 0x00; }
                        long now = DateTime.Now.ToFileTimeUtc();

                        ENTRY_FILE_STRUCTURE* file = (ENTRY_FILE_STRUCTURE*)ptr_buffer;
                        file->type = BLOCKTYPE.ENTRY_FOLDER;
                        file->prev_file = BLOCK_END;
                        file->next_file = BLOCK_END;
                        strcpy(file->filename, "\\");
                        file->offset_data = BLOCK_END;
                        file->filesize = BLOCK_END;
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
