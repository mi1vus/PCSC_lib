#pragma once
#include <windows.h>
#include "MainForm.h"

class DebitThread
{
public:
	void* ctx;
	_int64 TransID;
	bool (*debitCallback)(const _int64 TransactID, void* ctx);
	//CppWinForm1::MainForm *Form;
	void SetTransID(_int64 transID) { TransID = transID; }
	void Execute();
};