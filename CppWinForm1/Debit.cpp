#include "Debit.h"

//void* DebitThread::ctx;
//__int64 DebitThread::TransID;
//bool(*DebitThread::debitCallback)(const __int64 TransactID, void* ctx);

void DebitThread::Execute()
{
	__int64 transID;
	while (1)
	{
		if (transID != DebitThread::TransID)
		{
			transID = DebitThread::TransID;
			if (DebitThread::debitCallback != nullptr)
				DebitThread::debitCallback(transID, ctx);
		}
		Sleep(100);
	}
}