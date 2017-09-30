using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    class DiskManager
    {
        SafeFileHandle handle;
        public bool isActive { get; set; }

        public static string SIGNATURE = "WALLnut\x00";
        public static UInt64 BLOCK_SIZE = 0x1000;
        public static UInt64 HEADER_SIZE = 0x40;

        public DiskManager(string diskname)
        {
            handle = DiskIO.CreateFile(diskname, DiskIO.GENERIC_READ | DiskIO.GENERIC_WRITE, DiskIO.FILE_SHARE_READ | DiskIO.FILE_SHARE_WRITE, IntPtr.Zero, DiskIO.OPEN_EXISTING, 0, IntPtr.Zero);
            if (handle.IsInvalid == false && handle.IsClosed == false)
            {
                isActive = true;
            }
            else
            {
                isActive = false;
            }
        }
        
        #region [Funciton] 디스크를 인자로 받아 포맷합니다
        public static int FormatDisk(DiskInfo diskinfo)
        {
            unsafe
            {
                SafeFileHandle h = DiskIO.CreateFile(diskinfo.DeviceID, DiskIO.GENERIC_READ | DiskIO.GENERIC_WRITE, DiskIO.FILE_SHARE_READ | DiskIO.FILE_SHARE_WRITE, IntPtr.Zero, DiskIO.OPEN_EXISTING, 0, IntPtr.Zero);

                byte[] buf = new byte[512];
                uint[] read = new uint[1];
                fixed (byte* buffer = &buf[0])
                {
                    fixed (uint* readed = &read[0])
                    {
                        ulong offset = 0;

                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.ReadFile(h, buffer, 512, readed, IntPtr.Zero);

                        BinaryWriter bw = new BinaryWriter(File.Open(diskinfo.DeviceID + ".backup",FileMode.Create));
                        foreach(byte b in buf)
                        {
                            bw.Write(b);
                        }
                        bw.Close();

                        for (int i=0; i<8; i++) // 시그니처
                        {
                            buf[i] = Convert.ToByte(SIGNATURE[i]);
                        }
                        fixed(byte* ptr = &buf[0x08]) // 블록 사이즈 (기본 4096 byte)
                        {
                            *(UInt64*)ptr = BLOCK_SIZE;
                        }
                        fixed (byte* ptr = &buf[0x10]) // 파일 엔트리 (기본 오프셋 0xFF)
                        {
                            *(UInt64*)ptr = 0xFFFFFFFFFFFFFFFFL;
                        }
                        fixed (byte* ptr = &buf[0x18]) // 색인 엔트리 (기본 오프셋 0xFF)
                        {
                            *(UInt64*)ptr = 0xFFFFFFFFFFFFFFFFL;
                        }
                        fixed (byte* ptr = &buf[0x20]) // 비트맵 엔트리 (기본 오프셋 1)
                        {
                            *(UInt64*)ptr = 1 * BLOCK_SIZE;
                        }
                        fixed (byte* ptr = &buf[0x28]) // 디스크 크기 TODO
                        {
                            *(UInt64*)ptr = 1 * 0x1234;
                        }
                        fixed (byte* ptr = &buf[0x30]) // Reserved
                        {
                            *(UInt64*)ptr = 1 * 0x1234;
                        }
                        for (int i = 0; i < 8; i++) // END 시그니처
                        {
                            buf[0x38 + i] = Convert.ToByte(SIGNATURE[i]);
                        }



                        DiskIO.SetFilePointerEx(h, offset, out offset, DiskIO.FILE_BEGIN);
                        DiskIO.WriteFile(h, buffer, 512, readed, IntPtr.Zero);
                    }
                }
                DiskIO.CloseHandle(h);
            }
            return 0;
        }
        #endregion

        ~DiskManager()
        {
            DiskIO.CloseHandle(handle);
        }
    }
}
