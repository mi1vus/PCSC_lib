#include "stdafx.h"
#include "Debit.h"
#include <synchapi.h>
#include <iostream>

//void* DebitThread::ctx;
//__int64 DebitThread::TransID;
//bool(*DebitThread::debitCallback)(const __int64 TransactID, void* ctx);

void DebitThread::Execute()
{
	std::cout << "Execute:" << this->TransID;
	__int64 transID;
	while (true)
	{
		if (transID != this->TransID)
		{
			transID = this->TransID;
			if (this->debitCallback != 0)
				this->debitCallback(transID, ctx);
		}
		Sleep(100);
	}
}