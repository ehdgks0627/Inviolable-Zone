using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Windows;

namespace WALLnutClient
{
    class DiskManager
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

        public unsafe DiskManager(string diskname)
        {
            handle = DiskIO.CreateFile(diskname, DiskIO.GENERIC_READ | DiskIO.GENERIC_WRITE, DiskIO.FILE_SHARE_READ | DiskIO.FILE_SHARE_WRITE, IntPtr.Zero, DiskIO.OPEN_EXISTING, 0, IntPtr.Zero);
            if (!handle.IsInvalid && !handle.IsClosed)
            {
                byte[] buffer = new byte[BLOCK_SIZE];
                ReadBlock(ref buffer, 0);
                for(int i=0; i< 0x0008; i++)
                {
                    if(buffer[0x0000 + i] != SIGNATURE[i] || buffer[0x0038 + i] != SIGNATURE[i])
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
            return 0xFFFFFFFFFFFFFFFFL;
        }

        #region [Function] 디스크 offset 번째 블록에 buffer를 Read
        public unsafe void ReadBlock(ref byte[] buffer, UInt64 offset)
        {
            uint[] read = new uint[1];
            fixed(uint* ptr_read = &read[0])
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
        public bool SetBitMapBlock(UInt64 offset)
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            UInt64 block = 0x0014 + offset / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));

            return false;
        }
        #endregion

        #region [Function] 해당 디스크에서 비트맵 블록에서 offset의 비트 UnSet
        public bool UnSetBitMapBlock(UInt64 offset)
        {
            byte[] buffer = new byte[BLOCK_SIZE];
            UInt64 block = 0x0014 + offset / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));

            return false;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 찾기, Offset 반환
        public UInt64 FindFile(string filename)
        {

            return 0xFFFFFFFFFFFFFFFFL;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 저장 / 덮어쓰기
        public UInt64 SaveFile(string filename)
        {

            return 0xFFFFFFFFFFFFFFFFL;
        }
        #endregion

        #region [Function] path를 기준으로 파일을 삭제
        public UInt64 DeleteFile(string filename)
        {

            return 0xFFFFFFFFFFFFFFFFL;
        }
        #endregion

        #region [Function] 비트맵 블록에서 해당 오프셋의 비트를 Set
        private static bool SetBit(ref byte[] buffer, UInt64 offset)
        {
            UInt64 block = 0x0014 + offset / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));
            if ((buffer[block] & mask) == 0)
            {
                buffer[block] |= mask;
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [Function] 비트맵 블록에서 해당 오프셋의 비트를 UnSet
        private static bool UnSetBit(ref byte[] buffer, UInt64 offset)
        {
            UInt64 block = 0x0014 + offset / 8;
            byte mask = (byte)(1 << (byte)(offset % 8));
            if ((buffer[block] & mask) != 0)
            {
                buffer[block] ^= mask;
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

                        // 버퍼 초기화
                        for (uint i = 0; i < 0x1000; i++) { buffer[i] = 0x00; }
                        // 시그니처
                        for (uint i = 0; i < 0x0008; i++) { buffer[i] = Convert.ToByte(SIGNATURE[(int)i]); }
                        // 블록 사이즈 (기본 4096 byte)
                        fixed (byte* ptr = &buffer[0x0008]) { *(UInt64*)ptr = BLOCK_SIZE; }
                        // 파일 엔트리 (기본 오프셋 0xFF)
                        fixed (byte* ptr = &buffer[0x0010]) { *(UInt64*)ptr = 2 * BLOCK_SIZE; }
                        // 색인 엔트리 (기본 오프셋 0xFF)
                        fixed (byte* ptr = &buffer[0x0018]) { *(UInt64*)ptr = 0xFFFFFFFFFFFFFFFFL; }
                        // 비트맵 엔트리 (기본 오프셋 1)
                        fixed (byte* ptr = &buffer[0x0020]) { *(UInt64*)ptr = 1 * BLOCK_SIZE; }
                        // 디스크 크기
                        fixed (byte* ptr = &buffer[0x0028]) { *(UInt64*)ptr = diskinfo.Size; }
                        // Reserved
                        fixed (byte* ptr = &buffer[0x0030]) { *(UInt64*)ptr = 0x0000000000000000L; }
                        // END 시그니처
                        for (uint i = 0; i < 8; i++) { buffer[0x0038 + i] = Convert.ToByte(SIGNATURE[(int)i]); }
                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.WriteFile(h, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);
                        
                        // 초기 비트맵 생성
                        offset = 1 * BLOCK_SIZE;
                        for (uint i = 0; i < BLOCK_SIZE; i++) { buffer[i] = 0x00; }
                        fixed (byte* ptr = &buffer[0x0000]) { *(UInt32*)ptr = (UInt32)BLOCKTYPE.BITMAP; }
                        fixed (byte* ptr = &buffer[0x0004]) { *(UInt64*)ptr = BLOCK_END; }
                        fixed (byte* ptr = &buffer[0x000C]) { *(UInt64*)ptr = BLOCK_END; }
                        SetBit(ref buffer, 0x0000); //HEADER
                        SetBit(ref buffer, 0x0001); //BITMAP BLOCK

                        offset = 1 * BLOCK_SIZE;
                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.WriteFile(h, ptr_buffer, BLOCK_SIZE, ptr_read, IntPtr.Zero);
                        
                        // 초기 파일 엔트리 생성
                        long now = DateTime.Now.ToFileTimeUtc();
                        offset = 2 * BLOCK_SIZE;
                        for (uint i = 0; i < BLOCK_SIZE; i++) { buffer[i] = 0x00; }
                        fixed (byte* ptr = &buffer[0x0000]) { *(UInt32*)ptr = (UInt32)BLOCKTYPE.ENTRY_FOLDER; }
                        buffer[0x0004] = Convert.ToByte('\\');
                        fixed (byte* ptr = &buffer[0x0104]) { *(UInt64*)ptr = BLOCK_END; }
                        fixed (byte* ptr = &buffer[0x010C]) { *(UInt64*)ptr = BLOCK_END; }
                        fixed (byte* ptr = &buffer[0x0114]) { *(UInt64*)ptr = BLOCK_END; }
                        fixed (byte* ptr = &buffer[0x011C]) { *(Int64*)ptr = now; }
                        fixed (byte* ptr = &buffer[0x0124]) { *(Int64*)ptr = now; }
                        
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
