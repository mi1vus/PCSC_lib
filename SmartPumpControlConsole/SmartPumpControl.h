#pragma once
//#define _X86_
//#include "stdafx.h"
#include <SDKDDKVer.h>
#include <windows.h>
//#include <windef.h>
#include <fstream>
//#include <bemapiset.h>

//#pragma comment(lib, "MyDll.lib")
#ifdef UNICODE
#define LoadLibrary  LoadLibraryW
#else
#define LoadLibrary  LoadLibraryA
#endif // !UNICODE

#define SmartPumpControl_driver_dll "SmartPumpControl_Driver.dll"
#define SOpenFunc "Open"
#define SCloseProc "Close"
#define SDescriptionFunc "Description"
#define SFillingOverProc "FillingOver"
#define SCloseShiftProc "CloseShift"
#define SServiceProc "Service"
#define SSettingsProc "Settings"
#define SFuelPricesProc "FuelPrices"
#define SPumpFuelsProc "PumpFuels"
#define SGetCardTypes "GetCardTypes"
#define SGetTransactionFunc "GetTransaction"

struct TransactionInfo
{
	int Pump;
	int PaymentCode;
	int Fuel;
	int OrderInMoney;
	int Quantity;
	int Price;
	int Amount;
	char* CardNum;
	char* RRN;
	char* BillImage;
};

struct TDispStatusInfo
{
	char DispStatus;// Byte;
	__int16 StateFlags;// Word;
	int ErrorCode;// Integer;
	char DispMode;// Byte;
	char UpNozz;// Byte;
	char UpFuel;// Byte;
	char UpTank;// Byte;
	__int64 TransID;// Int64;
	char PreselMode;// Byte;
	double PreselDose;// Double;
	double PreselPice;// Double;
	char PreselFuel;// Byte;
	bool PreselFullTank;// ByteBool;
	double FillingVolume;// Double;
	double FillingPrice;// Double;
	double FillingSum;// Double;
};

typedef __int64(*TSetDoseDelegateFunc)(__int64 pump, __int64 osnovanie, void* ctx);
//TSetDoseDelegateFunc = function(const Pump, Osnovan : Longint; const Ctx : Pointer) : Int64; stdcall;

//public delegate IntPtr GetDose_Delegate(long Pump, IntPtr ctx);
typedef TDispStatusInfo* (*TGetDoseDelegateFunc)(__int64 pump, void* ctx);
//TGetDoseDelegateFunc = function(const Pump : Int64; const Ctx : Pointer) : PDispStatusInfo; stdcall;

//public delegate int CancelDose_Delegate(long TransID, IntPtr ctx);
typedef __int32(*TCancelDoseDelegateFunc)(__int64 TransID, void* ctx);
//TCancelDoseDelegateFunc = function(const TransID : Int64; const Ctx : Pointer) : Int32; stdcall;

//public delegate int HoldPump_Delegate(int Pump, byte ReleasePump, IntPtr ctx);
typedef __int32(*THoldPumpDelegateFunc)(__int32 Pump, char ReleasePump, void* ctx);
//THoldPumpDelegateFunc = function(const Pump : Int32; const ReleasePump : byte; const Ctx : Pointer) : Int32; stdcall;

typedef __int32(*TUpdateFillingOver_DelegateFunc)(__int32 Amount, __int32 Price, __int64 Trans_ID, __int32 DiscountMoney, void* ctx);
//TUpdateFillingOver_DelegateFunc = function(const Amount : Int32;
//const Price : Int32;
//const Trans_ID : Int64;
//const DiscountMoney : Int32;
//const Ctx : Pointer)
//	: Int32; stdcall;

//public delegate int InsertCardInfo_Delegate(long _DateTime,
//                             [MarshalAs(UnmanagedType.LPWStr)]string CardNo,
//                             int CardType, long Trans_ID, IntPtr ctx);
typedef __int32(*TInsertCardInfo_DelegateFunc)(
	double _DateTime,
	char* CardNo,
	__int32 CardType,
	__int64 Trans_ID,
	__int32 DiscountMoney,
	void* ctx);
//TInsertCardInfo_DelegateFunc = function(const _DateTime : TDateTime;
//const CardNo : PAnsiChar;
//const CardType : Int32;
//const Trans_ID : Int64;
//const Ctx : Pointer)
//	: Int32; stdcall;

typedef __int32(*TSaveReciept_DelegateFunc)(
	char* RecieptText,
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
//TSaveReciept_DelegateFunc = function(const RecieptText : PAnsiChar;
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
//	: Int32; stdcall;

typedef char(*TOpenFunc)(
	TSetDoseDelegateFunc SetCallback,
	TGetDoseDelegateFunc GetCallback,
	TCancelDoseDelegateFunc CancelCallback,
	THoldPumpDelegateFunc HoldPumpDelegate,
	TUpdateFillingOver_DelegateFunc UpdateFillingOver_Delegate,
	TInsertCardInfo_DelegateFunc InsertCardInfo_Delegate,
	TSaveReciept_DelegateFunc SaveReciept_Delegate,
	char* SystemName,
	void* ctx);
//TOpenFunc = function(const SetCallback : TSetDoseDelegateFunc;
//GetCallback:TGetDoseDelegateFunc;
//CancelCallback: TCancelDoseDelegateFunc;
//HoldPumpDelegate: THoldPumpDelegateFunc;
//UpdateFillingOver_Delegate:TUpdateFillingOver_DelegateFunc;
//InsertCardInfo_Delegate: TInsertCardInfo_DelegateFunc;
//SaveReciept_Delegate: TSaveReciept_DelegateFunc;
//SystemName: PAnsiChar;
//const Ctx : Pointer) : Byte; stdcall;

typedef void(*TCloseProc)();
//TCloseProc = procedure(); stdcall;
typedef char*(*TDescriptionFunc)();
//TDescriptionFunc = function() : PAnsiChar; stdcall;
typedef void(*TFillingOverProc)(__int64 TransNum, long Quantity, long Amount);
//TFillingOverProc = procedure(const TransNum : Int64; const Quantity, Amount : Longint); stdcall;
typedef void(*TCloseShiftProc)();
//TCloseShiftProc = procedure(); stdcall;
typedef void(*TServiceProc)();
//TServiceProc = procedure(); stdcall;
typedef void(*TSettingsProc)();
//TSettingsProc = procedure(); stdcall;
typedef void(*TFuelPricesProc)(char* Fuels);
//TFuelPricesProc = procedure(Fuels : PAnsiChar); stdcall;
typedef void(*TPumpFuelsProc)(char* PumpsInfo);
//TPumpFuelsProc = procedure(PumpsInfo : PAnsiChar); stdcall;
typedef char*(*TGetCardTypes)();
//TGetCardTypes = function() : PAnsiChar; stdcall;
typedef __int32(*TGetTransactionFunc)(__int64 ID, TransactionInfo* TransactionInfo);
//TGetTransactionFunc = function(const ID : Int64; const TransactionInfo : PTransactionInfo) : Int32; stdcall;

class SmartPumpControl
{
public:
	bool IsStatusOK;
	HMODULE Handle;// THandle;
	TOpenFunc Open;// TOpenFunc;
	TCloseProc Close;// TCloseProc;
	TDescriptionFunc Description;// TDescriptionFunc;
	TFillingOverProc FillingOver;// TFillingOverProc;
	TCloseShiftProc CloseShift;// TCloseShiftProc;
	TServiceProc Service;// TServiceProc;
	TSettingsProc Settings;// TSettingsProc;
	TFuelPricesProc FuelPrices;// TFuelPricesProc;
	TPumpFuelsProc PumpFuels;// TPumpFuelsProc;
	TGetCardTypes GetCardTypes;// TGetCardTypes;
	TGetTransactionFunc GetTransactionFunc;// TGetTransactionFunc

	SmartPumpControl(char* AFilePath, /*PSmartPumpControlLink* ALink, */ char* ADescription)
	{
		std::ifstream infile(AFilePath);
		IsStatusOK = infile.good();

		if (!IsStatusOK)
		{
			if (ADescription != nullptr)
				ADescription = strcat(strcat(ADescription, AFilePath), " библиотека не найдена!");
			return;
		}

		Handle = LoadLibrary((LPCWSTR)AFilePath);
		IsStatusOK = (Handle != nullptr);

		if (!IsStatusOK)
		{
			if (ADescription != nullptr)
				ADescription = strcat(strcat(ADescription, AFilePath), " не получается открыть библиотеку!");
			return;
		}

		try
		{
			(FARPROC &)Open = GetProcAddress(Handle, SOpenFunc);
			(FARPROC &)Close = GetProcAddress(Handle, SCloseProc);
			(FARPROC &)Description = GetProcAddress(Handle, SDescriptionFunc);
			(FARPROC &)FillingOver = GetProcAddress(Handle, SFillingOverProc);
			(FARPROC &)CloseShift = GetProcAddress(Handle, SCloseShiftProc);
			(FARPROC &)Service = GetProcAddress(Handle, SServiceProc);
			(FARPROC &)Settings = GetProcAddress(Handle, SSettingsProc);
			(FARPROC &)FuelPrices = GetProcAddress(Handle, SFuelPricesProc);
			(FARPROC &)PumpFuels = GetProcAddress(Handle, SPumpFuelsProc);
			(FARPROC &)GetCardTypes = GetProcAddress(Handle, SGetCardTypes);
			(FARPROC &)GetTransactionFunc = GetProcAddress(Handle, SGetTransactionFunc);

			IsStatusOK = VerifySmartPumpControl(/*ALink, */ADescription);
		}
		catch (...)
		{
			//if (!IsStatusOK)
			//	UnlinkSmartPumpControl(ALink);
		}
		//function from lib
		//void(*pFunction)(int, int);
		//(FARPROC &)pFunction = GetProcAddress(hLib, "Function");
		//pFunction(0, 0);
		//var from lib
		//int *pVar;
		//(FARPROC &)pVar = GetProcAddress(hLib, "Var");
		//*pVar = 123;
	}


private:
	bool VerifySmartPumpControl(/*const ALink : PSmartPumpControlLink; */char* ADescription)
	{
		//	IsStatusOK = Assigned(ALink.Open);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SOpenFunc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.Close);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SCloseProc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.Description);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SDescriptionFunc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.FillingOver);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SFillingOverProc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.CloseShift);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SCloseShiftProc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.Service);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SServiceProc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.Settings);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SSettingsProc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.FuelPrices);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SFuelPricesProc]);
		//		Exit;
		//		end;

		//	Result: = Assigned(ALink.PumpFuels);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SPumpFuelsProc]);
		//		Exit;
		//		end;
		//	Result: = Assigned(ALink.GetCardTypes);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SGetCardTypes]);
		//		Exit;
		//		end;
		//	Result: = Assigned(ALink.GetTransactionFunc);
		//		if not Result then begin
		//			if Assigned(ADescription) then
		//				ADescription^ : = Format(SFuncNotFound, [SGetTransactionFunc]);
		//		Exit;
		//		end;
		return IsStatusOK;
	}
};