#pragma once

class DebitThread
{
public:
	void* ctx;
	__int64 TransID;
	bool(*debitCallback)(const __int64 TransactID, void* ctx);
	//CppWinForm1::MainForm *Form;
	void SetTransID(__int64 transID) { TransID = transID; }
	void Execute();
};