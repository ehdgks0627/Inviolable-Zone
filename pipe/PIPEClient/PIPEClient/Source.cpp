#include <Windows.h>
#include <stdio.h>

int main()
{
	for (int i = 0; i < 512; i++)
	{
		HANDLE pipe = CreateFile(L"\\\\.\\pipe\\WALLnut", GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
		char buf[256] = { 0x41, };
		if (pipe == INVALID_HANDLE_VALUE)
		{
			printf("%d\n", i);
			printf("Error: %d\n", GetLastError());
			return 0;
		}
		WriteFile(pipe, buf, 1, NULL, NULL);
		CloseHandle(pipe);
	}
}