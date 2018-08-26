// SmartPumpControlConsole3.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include "SmartPumpControl.h"
#include "Debit.h"
#include <thread>

//typedef void(*AddText)(const char* Text);
//procedure AddText(Text: String);

typedef bool(*DebitOperation)(__int64 TransactID, void* ctx);
//class function DebitOperation(const TransactID : Int64;  const Ctx : Pointer) : Bool; stdcall; static;

//��������� callback ������� ������������ � �������
typedef __int64(*SetDoseDelegate)(__int32 Pump, __int32 Osnovan, void* ctx);
//class function SetDoseDelegate(const Pump, Osnovan : Longint; const Ctx : Pointer) : Int64; stdcall; static;
typedef TDispStatusInfo* (*GetDoseDelegate)(__int64 Pump, void* ctx);
//class function GetDoseDelegate(const Pump : Int64; const Ctx : Pointer) : PDispStatusInfo; stdcall; static;
typedef __int32(*CancelDoseDelegate)(__int64 TransID, void* ctx);
//class function CancelDoseDelegate(const TransID : Int64; const Ctx : Pointer) : Int32; stdcall; static;
typedef __int32(*HoldPumpDelegate)(__int32 Pump, char ReleasePump, void* ctx);
//class function HoldPumpDelegate(const Pump : Int32; const ReleasePump : byte; const Ctx : Pointer) : Int32; stdcall; static;

typedef __int32(*UpdateFillingOver_Delegate)(__int32 Amount,
	__int32 Price,
	__int64 Trans_ID,
	__int32 DiscountMoney,
	void* ctx);
//class function UpdateFillingOver_Delegate(const Amount : Int32;
//const Price : Int32;
//const Trans_ID : Int64;
//const DiscountMoney : Int32;
//const Ctx : Pointer)
//	: Int32; stdcall; static;

typedef __int32(*InsertCardInfo_Delegate)(double _DateTime,
	char* CardNo,
	__int32 CardType,
	__int64 Trans_ID,
	void* ctx);
//class function InsertCardInfo_Delegate(const _DateTime : TDateTime;
//const CardNo : PAnsiChar;
//const CardType : Int32;
//const Trans_ID : Int64;
//const Ctx : Pointer)
//	: Int32; stdcall; static;

typedef __int32(*SaveReciept_Delegate)(char* RecieptText,
	double _DateTime,
	char* DeviceName,
	char* DeviceSerial,
	__int32 DocNo,
	__int32 DocType,
	__int32 Amount,
	__int32 VarCheck,
	char* DocKind,
	__int32 DocKindCode,
	__int32 PayType,
	__int32 FactDoc,
	__int32 BP_Product,
	__int64 Trans_ID,
	void* ctx);
//class function SaveReciept_Delegate(const RecieptText : PAnsiChar;
//const _DateTime : TDateTime;
//const DeviceName : PAnsiChar;
//const DeviceSerial : PAnsiChar;
//const DocNo : Int32;
//const DocType : Int32;
//const Amount : Int32;
//const VarCheck : Int32;
//const DocKind : PAnsiChar;
//const DocKindCode : Int32;
//const PayType : Int32;
//const FactDoc : Int32;
//const BP_Product : Int32;
//const Trans_ID : Int64;
//const Ctx : Pointer)
//	: Int32; stdcall; static;
DebitThread* DebitTh;
SmartPumpControl* FSmartPumpControlLink;
__int64 TransCounter;
__int32 AmountMem, PriceMem, VolumeMem;

bool Open;
bool Close;
bool CloseShift;
bool Service;
bool Settings;
bool GetCardTypes;
bool FillingOver;

void AddText(const char* text)
{
	std::cout << text << std::endl;
}

bool DebOperation(__int64 TransactID, void* ctx)
{
	TransactionInfo* ATrans = new TransactionInfo();
	//	TransactionInfo FTrans;
	double _Price, _Quantity, _Amount;
	char *orderMode;

	// 	ATrans: = @FTrans;
	/*TSDIAppForm(Ctx).*/
	char buff[BuffSize];
	snprintf(buff, sizeof(buff), "��������� ���������� � ������, TransactID: %l", TransactID);
	AddText((char*)buff);
	if (FSmartPumpControlLink->GetTransactionFunc(TransactID, ATrans) == 1)
	{
		if (ATrans->OrderInMoney == 1)
		{
			orderMode = "�������� �����";
		}
		else
		{
			orderMode = "�������� �����";
		}
		_Price = ATrans->Price / 100;
		_Quantity = ATrans->Quantity / 1000;
		_Amount = ATrans->Amount / 100;

		for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		snprintf(buff, sizeof(buff),
			"���:            %i\r\n���������:      %i\r\n�������:        %i\r\n����� ������:   %s\r\n����������:     %f\r\n����:           %f\r\n�����:          %f\r\n����� �����:    %s\r\nRRN ����������: %s\r\n\r\n",
			ATrans->Pump, ATrans->PaymentCode, ATrans->Fuel, orderMode, _Quantity, _Price, _Amount, ATrans->CardNum, ATrans->RRN);
		AddText((char*)buff);

		//for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		//snprintf(buff, sizeof(buff),
		//"���:            "		   + IntToStr(ATrans.Pump)
		//+ #13#10 + "���������:      " + IntToStr(ATrans.PaymentCode)
		//+ #13#10 + "�������:        " + IntToStr(ATrans.Fuel)
		//+ #13#10 + "����� ������:   " + orderMode
		//+ #13#10 + "����������:     " + FloatToStr(_Quantity)
		//+ #13#10 + "����:           " + FloatToStr(_Price)
		//+ #13#10 + "�����:          " + FloatToStr(_Amount)
		//+ #13#10 + "����� �����:    " + ATrans.CardNum
		//+ #13#10 + "RRN ����������: " + ATrans.RRN + #13#10 + #13#10);

		AmountMem = ATrans->Amount;
		VolumeMem = ATrans->Quantity;
		PriceMem = ATrans->Price;
		//TSDIAppForm(Ctx).TransID_Edit.Text : = IntToStr(TransactID);
		//TSDIAppForm(Ctx).Price.Text : = FloatToStrF(_Price, ffFixed, 100, 2);
		//TSDIAppForm(Ctx).Volume.Text : = FloatToStrF(_Quantity, ffFixed, 100, 2);
		//TSDIAppForm(Ctx).Amount.Text : = FloatToStrF(_Amount, ffFixed, 100, 2);
		FillingOver = true;
		delete[] buff;
	}
}

__int64 SetDDelegate(__int64 Pump, __int64 Osnovan, void* Ctx)
{
	++TransCounter;

	char buff[BuffSize];
	snprintf(buff, sizeof(buff), "��������� ���� �� ���: %l , ������������ TransID = %l", Pump, TransCounter);
	AddText((char*)buff);
	//Windows.MessageBox(0, pchar('��������� ���� �� ���: ' + IntToStr(Pump) + ',#13#10���������' + IntToStr(Osnovan)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);

	DebitTh->SetTransID(TransCounter);

	return 	TransCounter;
	//if FSmartPumpControlLink.GetTransactionFunc(ARequestData.TransactID, ATrans) = 1
	//      then begin
}

TDispStatusInfo* GetDDelegate(__int64 Pump, void* ctx)
{
	bool _Res;
	char buff[BuffSize];
	snprintf(buff, sizeof(buff), "������ ��������� ���: %i", Pump);
	AddText((char*)buff);
	//AddText(pchar('������ ��������� ���: ' + IntToStr(Pump)));
	//Windows.MessageBox(0, pchar('������ ��������� ���: ' + IntToStr(Pump)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
	_Res = true;
	TDispStatusInfo *CRD_DISPSTATUS = new TDispStatusInfo();
	//try
	//{
	//	TDispStatusInfo *CRD_DISPSTATUS = new TDispStatusInfo();
	//}
	//catch (...)
	//{
	//	std::cout << "������ ��� ������� ��������� ���";
	//	_Res = false;
	//}

	if (_Res) {
		//{
		//DispStatus:
		//	0 - ��� ������(��� ���� TransID ������ = -1, ����� ������ ������ �������������� ��� 3)
		//		1 - ��� �������������
		//		3 - �������������� ������ �������
		//		10 - ��� ������
		//}
		CRD_DISPSTATUS->DispStatus = 0;
		// StateFlags - ������ 0
		CRD_DISPSTATUS->StateFlags = 0;
		// ErrorCode - ��� ������
		CRD_DISPSTATUS->ErrorCode = 0;
		// DispMode - ������ 0
		CRD_DISPSTATUS->DispMode = 0;
		// UpNozz - ����� ������� ���������, �� ������������ �������� ��� ����������
		// ����������� 0
		CRD_DISPSTATUS->UpNozz = 1;
		// UpFuel - ������� ������� ���������
		CRD_DISPSTATUS->UpFuel = 95;
		// UpTank - ����� �������, � ������� �������� ������ ��������
		//�� ������������ �������� ��� ���������� ����������� 0
		CRD_DISPSTATUS->UpTank = 0;
		// TransID - ����� ����������
		// � ������, ���� �� ��� ����������� �����: '-1'
		CRD_DISPSTATUS->TransID = -1;
		//PreselMode - ����� ������ �������������� �� ���
		//0 - �������� �����
		//1 - �������� �����
		CRD_DISPSTATUS->PreselMode = 0;
		//PreselDose - ����� ������ �������������� �� ���,
		//  � ������ 'PreselMode = 0' - ���-�� ������
		//  � ������ 'PreselMode = 1' - ����� � ������
		CRD_DISPSTATUS->PreselDose = 0;
		//PreselDose - ���� �� ��� ������� ��� ������ �������������� �� ���
		CRD_DISPSTATUS->PreselPice = 0;
		//PreselFuel - ������� ������ �������������� �� ���,
		CRD_DISPSTATUS->PreselFuel = 0;
		//PreselFullTank - ���� True, �� ��� ���������� ����� �� ������� ����
		CRD_DISPSTATUS->PreselFullTank = false;
		//FillingVolume - ������ ������� ��� ���-��.
		CRD_DISPSTATUS->FillingVolume = 0;
		//FillingVolume - ������ ������� ��� ����.
		CRD_DISPSTATUS->FillingPrice = 0;
		//FillingVolume - ������ ������� ��� �����.
		CRD_DISPSTATUS->FillingSum = 0;
	}
	return CRD_DISPSTATUS;//@GETDISPSTATUS.Answer;
}

__int32 CancelDDelegate(__int64 TransID, void* ctx)
{
	char buff[BuffSize];
	for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
	snprintf(buff, sizeof(buff), "������ ����������: TransactID: %ll", TransID);
	AddText((char*)buff);
	VolumeMem = 0;
	AmountMem = 0;
	FillingOver = false;
	FSmartPumpControlLink->FillingOver(TransCounter, VolumeMem, AmountMem);
	AddText("����� ������� ��������");
	PriceMem = 0;
	//Windows.MessageBox(0, pchar('������ ����������: ' + IntToStr(TransID)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
	return 1;
}

__int32 HoldPDelegate(__int32 Pump, char ReleasePump, void* ctx)
{
	char buff[BuffSize];
	if (ReleasePump == 0)
	{
		for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		snprintf(buff, sizeof(buff), "������ ���: %i", Pump);
		AddText((char*)buff);
		return 1;
		//Windows.MessageBox(0, pchar('������ ���: ' + IntToStr(Pump)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
	}
	else {
		for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		snprintf(buff, sizeof(buff), "������������ ���: ", Pump);
		AddText((char*)buff);
		return 1;
		//Windows.MessageBox(0, pchar('������������ ���: ' + IntToStr(Pump)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
	}
	return 0;
}

__int32 UpdateFOver_Delegate(__int32 Amount,
	__int32 Price,
	__int64 Trans_ID,
	__int32 DiscountMoney,
	void* ctx)
{
	char buff[BuffSize];
	snprintf(buff, sizeof(buff), "\r\n�������� ������, TransID: %i\r\n����� ����:   %f\r\n����� �����:  %f\r\n����� ������: %f",
		Trans_ID, (float)Price / 100, (float)Amount / 100, (float)DiscountMoney / 100);

	AddText((char*)buff);
	return 1;
}

__int32 InsertCInfo_Delegate(
	double _DateTime,
	char* CardNo,
	__int32 CardType,
	__int64 Trans_ID,
	__int32 DiscountMoney,
	void* ctx)
{
	char buff[BuffSize];
	snprintf(buff, sizeof(buff),
		"\r\n���������� ���������� � ��� �����, TransID: %ll\r\n ����/����� ����������: %f\r\n ����� �����:           %s\r\n��� �����:             %i", Trans_ID, _DateTime, CardNo, CardType);
	AddText((char*)buff);
	return 1;
}

__int32 SaveRcpt_Delegate(char* RecieptText,
	double _DateTime,
	char* DeviceName,
	char* DeviceSerial,
	__int32 DocNo,
	__int32 DocType,
	__int32 Amount,
	__int32 VarCheck,
	char* DocKind,
	__int32 DocKindCode,
	__int32 PayType,
	__int32 FactDoc,
	__int32 BP_Product,
	__int64 Trans_ID,
	void* ctx)
{
	char buff[BuffSize];
	snprintf(buff, sizeof(buff),
		"���������� ���������, TransID: %i\r\n����/�����:         %f\r\n��� ����������:     %s\r\n�������� �����:     %s\r\n����� ���������:    %i\r\n��� ���������:      %i\r\n�����:              %i\r\n������������ ���:   %i\r\n��� ���������:      %s\r\n��� ���� ���������: %i\r\n��� ������:         %i\r\n��� �� �����:       %i\r\n����� ��������:     %i\r\nID ����������:      %i\r\n------------------------------------------------------\r\n����� ����:      \r\n%s\r\n------------------------------------------------------\r\n",
		Trans_ID, _DateTime, DeviceName, DeviceSerial, DocNo, DocType, Amount, VarCheck, DocKind, DocKindCode, PayType, FactDoc, BP_Product, Trans_ID, RecieptText);
	AddText((char*)buff);
	//AddText(
	//		'���������� ���������, TransID: ' + IntToStr(Trans_ID)
	//		+ #13#10 + '����/�����:         ' + DateTimeToStr(_DateTime)
	//		+ #13#10 + '��� ����������:     ' + DeviceName
	//		+ #13#10 + '�������� �����:     ' + DeviceSerial
	//		+ #13#10 + '����� ���������:    ' + IntToStr(DocNo)
	//		+ #13#10 + '��� ���������:      ' + IntToStr(DocType)
	//		+ #13#10 + '�����:              ' + IntToStr(Amount)
	//		+ #13#10 + '������������ ���:   ' + IntToStr(VarCheck)
	//		+ #13#10 + '��� ���������:      ' + DocKind
	//		+ #13#10 + '��� ���� ���������: ' + IntToStr(DocKindCode)
	//		+ #13#10 + '��� ������:         ' + IntToStr(PayType)
	//		+ #13#10 + '��� �� �����:       ' + IntToStr(FactDoc)
	//		+ #13#10 + '����� ��������:     ' + IntToStr(BP_Product)
	//		+ #13#10 + 'ID ����������:      ' + IntToStr(Trans_ID)
	//		+ #13#10 + '------------------------------------------------------'
	//		+ #13#10 + '����� ����:      '
	//		+ #13#10 + RecieptText
	//		+ #13#10 + '------------------------------------------------------'
	//	);
	return 1;
}

int _tmain(int argc, _TCHAR* argv[])
{
	Open = true;
	int menu = 0;
	std::cout << "�������� ��������:\n\r";

	if (Open)
		std::cout << "1) ������� ����������\n\r";
	if (Close)
		std::cout << "2) ������� ����������\n\r";
	if (CloseShift)
		std::cout << "3) ������� �����\n\r";
	if (Service)
		std::cout << "4) ������\n\r";
	if (Settings)
		std::cout << "5) ���������\n\r";
	if (GetCardTypes)
		std::cout << "6) ������� ����������\n\r";
	if (FillingOver)
		std::cout << "7) ��������� �����\n\r";
	std::cin >> menu;

	if (menu == 1)
	{
		try
		{
			DebitTh = new DebitThread();
			DebitTh->debitCallback = &DebOperation;
			DebitTh->ctx = 0;
			//			std::thread t1(&DebitThread::Execute);

			// wait for return
			//t1.join();
			std::cout << "��������� �������\n\r";

			//��������� �������
			char* msg = new char[3000];
			FSmartPumpControlLink = new SmartPumpControl("�:\\SmartPumpControl_Driver.dll", msg);
			if (!FSmartPumpControlLink->IsStatusOK)
				std::cout << "������ ����������� ����������";

			std::cout << "��������� ������������� ��������\n";
			//��������� ������� �� � �������� ������� Open � ��������� ������������� �������� ����������� �������� � ������� Open
			//callback ������� � ������ �� ������ � �� �������� ����������� �������������. � ���������� ������ �� ������ ������
			//����� ������������ ������� ��� ������ ������ calback �������.
			if (FSmartPumpControlLink->Open != 0)
			{
				if (FSmartPumpControlLink->Open(
					&SetDDelegate, &GetDDelegate,
					&CancelDDelegate, &HoldPDelegate,
					&UpdateFOver_Delegate, &InsertCInfo_Delegate,
					&SaveRcpt_Delegate, "Sample Control", 0) != 1)
				{
					std::cout << "������ ����������� ����������";
					std::cin >> menu;
					return 0;
				}
				else
				{
					std::cout << FSmartPumpControlLink->Description();
				}
			}
			//��������� ������� �� � �������� ������� FuelPrices � ��������� ��������� ��� �� �������. ����� ��� ������ ����� ����
			//���������� ������������� �������� FuelPrices � ���������� ����. ��� ���� ��� ��������� ���� ����� ����, ����������
			//��������� ���� ������ ���.
			/*{
			���� ���������� � ���� ������ � ������������� :
			�������: ';',
			����� : '=',
			� ��������� ������� :

			���_�������� = ������������_����_������� = ���� = ����������_�����_��������_�_�������;

			��������:
			80 = �� - 80 = 28, 8 = 3

			80 - ��� ��������
			�� - 80 - ������������ ��������
			28, 8 - ���� ������ ����� ��������
			3 - ���������� ����� �������� � �������
			}
			*/
			AddText("��������� ��������� ��� �� �������");
			if (FSmartPumpControlLink->FuelPrices != 0)
				FSmartPumpControlLink->FuelPrices("95=��-95-�5=36,2=1;92=��-92-�5=32,23=2;80=��-80=28,8=3");

			//��������� ������� �� � �������� ������� PumpFuels � ��������� �������� ��� � ����� �������.
			/*{
			������������  ���������� � ���� ������ � ������������� :
			�������: ';',
			����� : '=',
			������ ������ ���� : ',',
			� ��������� ������� :

			�����_��� = ���_��������_1, ���_��������_2, ..., ���_��������_n;

			��������:
			1 = 95, 92, 80
			1 - ����� ���
			95, 92, 80 - ���� ��������� ��������� �� ������ ���
			}
			*/
			AddText("��������� �������� ��� � ����� �������");
			if (FSmartPumpControlLink->PumpFuels != 0)
				FSmartPumpControlLink->PumpFuels("1=95,92,80;2=95,92,80;3=95,92;4=95,92");

			Open = false;
			Close = true;
			CloseShift = true;
			Service = true;
			Settings = true;
			GetCardTypes = true;

		}
		catch (...)
		{
			std::cout << "������ ������������� ����������";
		}
	}
	else if (menu == 2)
	{
		FSmartPumpControlLink->Close();
		Open = true;
		Close = false;
		CloseShift = false;
		Service = false;
		Settings = false;
		GetCardTypes = false;
	}
	else if (menu == 3)
	{
		FSmartPumpControlLink->CloseShift();
	}
	else if (menu == 4)
	{
		FSmartPumpControlLink->Service();
	}
	else if (menu == 5)
	{
		FSmartPumpControlLink->Settings();
	}
	else if (menu == 6)
	{
		AddText(FSmartPumpControlLink->GetCardTypes());
	}
	else if (menu == 7)
	{
		FillingOver = false;
		FSmartPumpControlLink->FillingOver(TransCounter, VolumeMem, AmountMem);
		AddText("����� ������� ��������");
		PriceMem = 0;
	}
}