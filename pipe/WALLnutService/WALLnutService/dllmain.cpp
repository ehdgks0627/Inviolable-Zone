#include "windows.h"
#include "stdio.h"
#include "tchar.h"
#include "types.h"
#include "process.h"

#define _CRT_SECURE_NO_WARNINGS
//#define DEBUG_FUNC_CALL

/*
TODO
64bit
logging
*/

BYTE g_pOrgZwCreateFile[5] = { 0, };
BYTE g_pOrgZwOpenFile[5] = { 0, };
BYTE g_pOrgZwReadFile[5] = { 0, };
BYTE g_pOrgZwWriteFile[5] = { 0, };
BYTE g_pOrgZwClose[5] = { 0, };

BYTE originZwCreateFile[18] = { 0, };
BYTE originZwOpenFile[18] = { 0, };
BYTE originZwReadFile[18] = { 0, };
BYTE originZwWriteFile[18] = { 0, };
BYTE originZwCloseFile[18] = { 0, };

HANDLE pipeClient = INVALID_HANDLE_VALUE;
int pid = getpid();

void makeLog(char *buf, char *funcName)
{
	sprintf(buf, "%d | %s", pid, funcName);
}

BOOL hook_by_code(LPCSTR szDllName, LPCSTR szFuncName, PROC pfnNew, PBYTE pOrgBytes)
{
	FARPROC pFunc;
	DWORD dwOldProtect, dwAddress;
	BYTE pBuf[5] = { 0xE9, 0, };
	PBYTE pByte;

	pFunc = (FARPROC)GetProcAddress(GetModuleHandleA(szDllName), szFuncName);

	pByte = (PBYTE)pFunc;
	if (pByte[0] == 0xE9)
		return FALSE;
	VirtualProtect((LPVOID)pOrgBytes, 18, PAGE_EXECUTE_READWRITE, &dwOldProtect);
	VirtualProtect((LPVOID)pFunc, 5, PAGE_EXECUTE_READWRITE, &dwOldProtect);

	memcpy(pOrgBytes, pFunc, 18);
	dwAddress = (DWORD)pOrgBytes - (DWORD)pFunc - 5;
	memcpy(&pBuf[1], &dwAddress, 4);

	memcpy(pFunc, pBuf, 5);

	VirtualProtect((LPVOID)pFunc, 5, dwOldProtect, &dwOldProtect);

	return TRUE;
}

BOOL unhook_by_code(LPCSTR szDllName, LPCSTR szFuncName, PBYTE pOrgBytes)
{
	FARPROC pFunc;
	DWORD dwOldProtect;
	PBYTE pByte;

	pFunc = (FARPROC)GetProcAddress(GetModuleHandleA(szDllName), szFuncName);
	pByte = (PBYTE)pFunc;
	if (pByte[0] != 0xE9)
		return FALSE;

	VirtualProtect((LPVOID)pFunc, 5, PAGE_EXECUTE_READWRITE, &dwOldProtect);

	memcpy(pFunc, pOrgBytes, 5);

	VirtualProtect((LPVOID)pFunc, 5, dwOldProtect, &dwOldProtect);

	return TRUE;
}

BOOL SetPrivilege(LPCTSTR lpszPrivilege, BOOL bEnablePrivilege)
{
	TOKEN_PRIVILEGES tp;
	HANDLE hToken;
	LUID luid;

	if (!OpenProcessToken(GetCurrentProcess(),
		TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
		&hToken))
	{
		printf("OpenProcessToken error: %u\n", GetLastError());
		return FALSE;
	}

	if (!LookupPrivilegeValue(NULL,
		lpszPrivilege,
		&luid))
	{
		printf("LookupPrivilegeValue error: %u\n", GetLastError());
		return FALSE;
	}

	tp.PrivilegeCount = 1;
	tp.Privileges[0].Luid = luid;
	if (bEnablePrivilege)
		tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
	else
		tp.Privileges[0].Attributes = 0;

	if (!AdjustTokenPrivileges(hToken,
		FALSE,
		&tp,
		sizeof(TOKEN_PRIVILEGES),
		(PTOKEN_PRIVILEGES)NULL,
		(PDWORD)NULL))
	{
		printf("AdjustTokenPrivileges error: %u\n", GetLastError());
		return FALSE;
	}

	if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)
	{
		printf("The token does not have the specified privilege. \n");
		return FALSE;
	}

	return TRUE;
}

NTSTATUS WINAPI NewZwCreateFile(
	_Out_    PHANDLE            FileHandle,
	_In_     ACCESS_MASK        DesiredAccess,
	_In_     POBJECT_ATTRIBUTES ObjectAttributes,
	_Out_    PIO_STATUS_BLOCK   IoStatusBlock,
	_In_opt_ PLARGE_INTEGER     AllocationSize,
	_In_     ULONG              FileAttributes,
	_In_     ULONG              ShareAccess,
	_In_     ULONG              CreateDisposition,
	_In_     ULONG              CreateOptions,
	_In_     PVOID              EaBuffer,
	_In_     ULONG              EaLength)
{
	NTSTATUS status;
	FARPROC pFunc;
	char szProcName[MAX_PATH] = { 0, };
	char buf[256];
	makeLog(buf, "ZwCreateFile");
	WriteFile(pipeClient, buf, strlen(buf), NULL, NULL);

	unhook_by_code("ntdll.dll", "ZwCreateFile", g_pOrgZwCreateFile);
	
	pFunc = GetProcAddress(GetModuleHandleA("ntdll.dll"),
		"ZwCreateFile");

#ifdef DEBUG_FUNC_CALL
	OutputDebugStringW(L"ZwCreateFile Called");
#endif

	status = ((PFZWCREATEFILE)pFunc)
		(FileHandle,
			DesiredAccess,
			ObjectAttributes,
			IoStatusBlock,
			AllocationSize,
			FileAttributes,
			ShareAccess,
			CreateDisposition,
			CreateOptions,
			EaBuffer,
			EaLength);

	if (status != STATUS_SUCCESS)
		goto __NTQUERYSYSTEMINFORMATION_END;

__NTQUERYSYSTEMINFORMATION_END:

	hook_by_code("ntdll.dll", "ZwCreateFile",
		(PROC)NewZwCreateFile, g_pOrgZwCreateFile);

	return status;
}

NTSTATUS WINAPI NewZwWriteFile(
	_In_     HANDLE           FileHandle,
	_In_opt_ HANDLE           Event,
	_In_opt_ PIO_APC_ROUTINE  ApcRoutine,
	_In_opt_ PVOID            ApcContext,
	_Out_    PIO_STATUS_BLOCK IoStatusBlock,
	_Out_    PVOID            Buffer,
	_In_     ULONG            Length,
	_In_opt_ PLARGE_INTEGER   ByteOffset,
	_In_opt_ PULONG           Key)
{
	NTSTATUS status;
	FARPROC pFunc;
	char szProcName[MAX_PATH] = { 0, };
	char buf[256];

	makeLog(buf, "ZwWriteFile");
	WriteFile(pipeClient, buf, strlen(buf), NULL, NULL);

	unhook_by_code("ntdll.dll", "ZwWriteFile", g_pOrgZwWriteFile);

	pFunc = GetProcAddress(GetModuleHandleA("ntdll.dll"),
		"ZwWriteFile");

#ifdef DEBUG_FUNC_CALL
	OutputDebugStringW(L"ZwWriteFile Called");
#endif

	status = ((PFZWWRITEFILE)pFunc)
		(FileHandle,
			Event,
			ApcRoutine,
			ApcContext,
			IoStatusBlock,
			Buffer,
			Length,
			ByteOffset,
			Key);

	if (status != STATUS_SUCCESS)
		goto __NTQUERYSYSTEMINFORMATION_END;

__NTQUERYSYSTEMINFORMATION_END:

	hook_by_code("ntdll.dll", "ZwWriteFile",
		(PROC)NewZwWriteFile, g_pOrgZwWriteFile);

	return status;
}

NTSTATUS WINAPI NewZwReadFile(
	_In_     HANDLE           FileHandle,
	_In_opt_ HANDLE           Event,
	_In_opt_ PIO_APC_ROUTINE  ApcRoutine,
	_In_opt_ PVOID            ApcContext,
	_Out_    PIO_STATUS_BLOCK IoStatusBlock,
	_Out_    PVOID            Buffer,
	_In_     ULONG            Length,
	_In_opt_ PLARGE_INTEGER   ByteOffset,
	_In_opt_ PULONG           Key)
{
	NTSTATUS status;
	FARPROC pFunc;
	char szProcName[MAX_PATH] = { 0, };
	char buf[256];

	makeLog(buf, "ZwReadFile");
	WriteFile(pipeClient, buf, strlen(buf), NULL, NULL);

	unhook_by_code("ntdll.dll", "ZwReadFile", g_pOrgZwReadFile);

	pFunc = GetProcAddress(GetModuleHandleA("ntdll.dll"),
		"ZwReadFile");

#ifdef DEBUG_FUNC_CALL
	OutputDebugStringW(L"ZwReadFile Called");
#endif

	status = ((PFZWREADFILE)pFunc)
		(FileHandle,
			Event,
			ApcRoutine,
			ApcContext,
			IoStatusBlock,
			Buffer,
			Length,
			ByteOffset,
			Key);

	if (status != STATUS_SUCCESS)
		goto __NTQUERYSYSTEMINFORMATION_END;

__NTQUERYSYSTEMINFORMATION_END:

	hook_by_code("ntdll.dll", "ZwReadFile",
		(PROC)NewZwReadFile, g_pOrgZwReadFile);

	return status;
}

NTSTATUS WINAPI NewZwClose(
	_In_     HANDLE           FileHandle)
{
	NTSTATUS status;
	FARPROC pFunc;
	char szProcName[MAX_PATH] = { 0, };
	char buf[256];

	makeLog(buf, "ZwClose");
	WriteFile(pipeClient, buf, strlen(buf), NULL, NULL);

	unhook_by_code("ntdll.dll", "ZwClose", g_pOrgZwClose);

	pFunc = GetProcAddress(GetModuleHandleA("ntdll.dll"),
		"ZwClose");

#ifdef DEBUG_FUNC_CALL
	OutputDebugStringW(L"ZwClose Called");
#endif

	status = ((PFZWCLOSE)pFunc)
		(FileHandle);

	if (status != STATUS_SUCCESS)
		goto __NTQUERYSYSTEMINFORMATION_END;

__NTQUERYSYSTEMINFORMATION_END:

	hook_by_code("ntdll.dll", "ZwClose",
		(PROC)NewZwClose, g_pOrgZwClose);

	return status;
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	char            szCurProc[MAX_PATH] = { 0, };
	char            *p = NULL;
	char            buf[256] = { 0, };

	GetModuleFileNameA(NULL, szCurProc, MAX_PATH);
	p = strrchr(szCurProc, '\\');
	if ((p != NULL) && !_stricmp(p + 1, "HideProc2.exe"))
		return TRUE;

	SetPrivilege(SE_DEBUG_NAME, TRUE);

	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
		pipeClient = CreateFileA("\\\\.\\pipe\\WALLnut", GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
		if (pipeClient == INVALID_HANDLE_VALUE)
		{
			return FALSE;
		}
		makeLog(buf, "DLL_PROCESS_ATTACH\n");
		WriteFile(pipeClient, buf, strlen(buf), NULL, NULL);

		hook_by_code("ntdll.dll", "ZwCreateFile", (PROC)NULL, originZwCreateFile);
		hook_by_code("ntdll.dll", "ZwOpenFile", (PROC)NULL, originZwOpenFile);
		hook_by_code("ntdll.dll", "ZwWriteFile", (PROC)NULL, originZwReadFile);
		hook_by_code("ntdll.dll", "ZwReadFile", (PROC)NULL, originZwWriteFile);
		hook_by_code("ntdll.dll", "ZwClose", (PROC)NULL, originZwCloseFile);
		break;

	case DLL_PROCESS_DETACH:
		unhook_by_code("ntdll.dll", "ZwCreateFile", originZwCreateFile);
		unhook_by_code("ntdll.dll", "ZwOpenFile", originZwOpenFile);
		unhook_by_code("ntdll.dll", "ZwWriteFile", originZwWriteFile);
		unhook_by_code("ntdll.dll", "ZwReadFile", originZwReadFile);
		unhook_by_code("ntdll.dll", "ZwClose", originZwCloseFile);
		OutputDebugStringA(itoa(pid, buf, 10));
		if (pipeClient != INVALID_HANDLE_VALUE)
		{
			makeLog(buf, "DLL_PROCESS_DETACH\n");
			WriteFile(pipeClient, buf, strlen(buf), NULL, NULL);
			WriteFile(pipeClient, "\xFF", 1, NULL, NULL);
			CloseHandle(pipeClient);
		}
		break;
	}

	return TRUE;
}
