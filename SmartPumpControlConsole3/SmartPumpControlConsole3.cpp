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

//Объявляем callback функции передаваемые в драйвер
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
	snprintf(buff, sizeof(buff), "Получение ниформации о заказе, TransactID: %l", TransactID);
	AddText((char*)buff);
	if (FSmartPumpControlLink->GetTransactionFunc(TransactID, ATrans) == 1)
	{
		if (ATrans->OrderInMoney == 1)
		{
			orderMode = "Денежный заказ";
		}
		else
		{
			orderMode = "Литровый заказ";
		}
		_Price = ATrans->Price / 100;
		_Quantity = ATrans->Quantity / 1000;
		_Amount = ATrans->Amount / 100;

		for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		snprintf(buff, sizeof(buff),
			"ТРК:            %i\r\nОснование:      %i\r\nПродукт:        %i\r\nРежим заказа:   %s\r\nКоличество:     %f\r\nЦена:           %f\r\nСумма:          %f\r\nНомер карты:    %s\r\nRRN Транзакции: %s\r\n\r\n",
			ATrans->Pump, ATrans->PaymentCode, ATrans->Fuel, orderMode, _Quantity, _Price, _Amount, ATrans->CardNum, ATrans->RRN);
		AddText((char*)buff);

		//for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		//snprintf(buff, sizeof(buff),
		//"ТРК:            "		   + IntToStr(ATrans.Pump)
		//+ #13#10 + "Основание:      " + IntToStr(ATrans.PaymentCode)
		//+ #13#10 + "Продукт:        " + IntToStr(ATrans.Fuel)
		//+ #13#10 + "Режим заказа:   " + orderMode
		//+ #13#10 + "Количество:     " + FloatToStr(_Quantity)
		//+ #13#10 + "Цена:           " + FloatToStr(_Price)
		//+ #13#10 + "Сумма:          " + FloatToStr(_Amount)
		//+ #13#10 + "Номер карты:    " + ATrans.CardNum
		//+ #13#10 + "RRN Транзакции: " + ATrans.RRN + #13#10 + #13#10);

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
	snprintf(buff, sizeof(buff), "Установка дозы на ТРК: %l , сгенерирован TransID = %l", Pump, TransCounter);
	AddText((char*)buff);
	//Windows.MessageBox(0, pchar('Установка дозы на ТРК: ' + IntToStr(Pump) + ',#13#10Основание' + IntToStr(Osnovan)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);

	DebitTh->SetTransID(TransCounter);

	return 	TransCounter;
	//if FSmartPumpControlLink.GetTransactionFunc(ARequestData.TransactID, ATrans) = 1
	//      then begin
}

TDispStatusInfo* GetDDelegate(__int64 Pump, void* ctx)
{
	bool _Res;
	char buff[BuffSize];
	snprintf(buff, sizeof(buff), "Запрос состояние ТРК: %i", Pump);
	AddText((char*)buff);
	//AddText(pchar('Запрос состояние ТРК: ' + IntToStr(Pump)));
	//Windows.MessageBox(0, pchar('Запрос состояние ТРК: ' + IntToStr(Pump)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
	_Res = true;
	TDispStatusInfo *CRD_DISPSTATUS = new TDispStatusInfo();
	//try
	//{
	//	TDispStatusInfo *CRD_DISPSTATUS = new TDispStatusInfo();
	//}
	//catch (...)
	//{
	//	std::cout << "Ошибка при запросе состояния ТРК";
	//	_Res = false;
	//}

	if (_Res) {
		//{
		//DispStatus:
		//	0 - ТРК онлайн(при этом TransID должен = -1, иначе данный статус воспринимается как 3)
		//		1 - ТРК заблокирована
		//		3 - Осуществляется отпуск топлива
		//		10 - ТРК занята
		//}
		CRD_DISPSTATUS->DispStatus = 0;
		// StateFlags - всегда 0
		CRD_DISPSTATUS->StateFlags = 0;
		// ErrorCode - код ошибки
		CRD_DISPSTATUS->ErrorCode = 0;
		// DispMode - всегда 0
		CRD_DISPSTATUS->DispMode = 0;
		// UpNozz - номер снятого пистолета, не обязательный параметр для заполнения
		// допускается 0
		CRD_DISPSTATUS->UpNozz = 1;
		// UpFuel - продукт снятого пистолета
		CRD_DISPSTATUS->UpFuel = 95;
		// UpTank - Номер емкости, к которой привязан снятый пистолет
		//не обязательный параметр для заполнения допускается 0
		CRD_DISPSTATUS->UpTank = 0;
		// TransID - Номер транзакции
		// в случае, если на ТРК отсутствует заказ: '-1'
		CRD_DISPSTATUS->TransID = -1;
		//PreselMode - режим заказа установленного на ТРК
		//0 - литровый заказ
		//1 - денежный заказ
		CRD_DISPSTATUS->PreselMode = 0;
		//PreselDose - сумма заказа установленного на ТРК,
		//  в случае 'PreselMode = 0' - кол-во литров
		//  в случае 'PreselMode = 1' - сумма в рублях
		CRD_DISPSTATUS->PreselDose = 0;
		//PreselDose - цена за лит топлива для заказа установленного на ТРК
		CRD_DISPSTATUS->PreselPice = 0;
		//PreselFuel - продукт заказа установленного на ТРК,
		CRD_DISPSTATUS->PreselFuel = 0;
		//PreselFullTank - если True, на ТРК установлен заказ до полного бака
		CRD_DISPSTATUS->PreselFullTank = false;
		//FillingVolume - данные дисплея ТРК кол-во.
		CRD_DISPSTATUS->FillingVolume = 0;
		//FillingVolume - данные дисплея ТРК цена.
		CRD_DISPSTATUS->FillingPrice = 0;
		//FillingVolume - данные дисплея ТРК сумма.
		CRD_DISPSTATUS->FillingSum = 0;
	}
	return CRD_DISPSTATUS;//@GETDISPSTATUS.Answer;
}

__int32 CancelDDelegate(__int64 TransID, void* ctx)
{
	char buff[BuffSize];
	for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
	snprintf(buff, sizeof(buff), "Отмена транзакции: TransactID: %ll", TransID);
	AddText((char*)buff);
	VolumeMem = 0;
	AmountMem = 0;
	FillingOver = false;
	FSmartPumpControlLink->FillingOver(TransCounter, VolumeMem, AmountMem);
	AddText("Налив успешно завершен");
	PriceMem = 0;
	//Windows.MessageBox(0, pchar('Отмена транзакции: ' + IntToStr(TransID)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
	return 1;
}

__int32 HoldPDelegate(__int32 Pump, char ReleasePump, void* ctx)
{
	char buff[BuffSize];
	if (ReleasePump == 0)
	{
		for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		snprintf(buff, sizeof(buff), "Захват ТРК: %i", Pump);
		AddText((char*)buff);
		return 1;
		//Windows.MessageBox(0, pchar('Захват ТРК: ' + IntToStr(Pump)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
	}
	else {
		for (int i = 0; i < BuffSize; ++i) buff[i] = 0;
		snprintf(buff, sizeof(buff), "Освобождение ТРК: ", Pump);
		AddText((char*)buff);
		return 1;
		//Windows.MessageBox(0, pchar('Освобождение ТРК: ' + IntToStr(Pump)), 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
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
	snprintf(buff, sizeof(buff), "\r\nПересчет заказа, TransID: %i\r\nНовая цена:   %f\r\nНовая сумма:  %f\r\nНовая скидка: %f",
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
		"\r\nСохранение информации о доп карте, TransID: %ll\r\n Дата/Время транзакции: %f\r\n Номер карты:           %s\r\nТип карты:             %i", Trans_ID, _DateTime, CardNo, CardType);
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
		"Сохранение документа, TransID: %i\r\nДата/время:         %f\r\nИмя устройства:     %s\r\nСерийный номер:     %s\r\nНомер документа:    %i\r\nТип документа:      %i\r\nСумма:              %i\r\nПроизвольный чек:   %i\r\nВид документа:      %s\r\nКод вида документа: %i\r\nТип оплаты:         %i\r\nЧек по факту:       %i\r\nНомер продукта:     %i\r\nID Транзакции:      %i\r\n------------------------------------------------------\r\nОбраз Чека:      \r\n%s\r\n------------------------------------------------------\r\n",
		Trans_ID, _DateTime, DeviceName, DeviceSerial, DocNo, DocType, Amount, VarCheck, DocKind, DocKindCode, PayType, FactDoc, BP_Product, Trans_ID, RecieptText);
	AddText((char*)buff);
	//AddText(
	//		'Сохранение документа, TransID: ' + IntToStr(Trans_ID)
	//		+ #13#10 + 'Дата/время:         ' + DateTimeToStr(_DateTime)
	//		+ #13#10 + 'Имя устройства:     ' + DeviceName
	//		+ #13#10 + 'Серийный номер:     ' + DeviceSerial
	//		+ #13#10 + 'Номер документа:    ' + IntToStr(DocNo)
	//		+ #13#10 + 'Тип документа:      ' + IntToStr(DocType)
	//		+ #13#10 + 'Сумма:              ' + IntToStr(Amount)
	//		+ #13#10 + 'Произвольный чек:   ' + IntToStr(VarCheck)
	//		+ #13#10 + 'Вид документа:      ' + DocKind
	//		+ #13#10 + 'Код вида документа: ' + IntToStr(DocKindCode)
	//		+ #13#10 + 'Тип оплаты:         ' + IntToStr(PayType)
	//		+ #13#10 + 'Чек по факту:       ' + IntToStr(FactDoc)
	//		+ #13#10 + 'Номер продукта:     ' + IntToStr(BP_Product)
	//		+ #13#10 + 'ID Транзакции:      ' + IntToStr(Trans_ID)
	//		+ #13#10 + '------------------------------------------------------'
	//		+ #13#10 + 'Образ Чека:      '
	//		+ #13#10 + RecieptText
	//		+ #13#10 + '------------------------------------------------------'
	//	);
	return 1;
}

int _tmain(int argc, _TCHAR* argv[])
{
	Open = true;
	int menu = 0;
	std::cout << "Выберите действие:\n\r";

	if (Open)
		std::cout << "1) Открыть библиотеку\n\r";
	if (Close)
		std::cout << "2) Закрыть библиотеку\n\r";
	if (CloseShift)
		std::cout << "3) Закрыть смену\n\r";
	if (Service)
		std::cout << "4) Сервис\n\r";
	if (Settings)
		std::cout << "5) Настройки\n\r";
	if (GetCardTypes)
		std::cout << "6) Открыть библиотеку\n\r";
	if (FillingOver)
		std::cout << "7) Завершить налив\n\r";
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
			std::cout << "Загружаем драйвер\n\r";

			//Загружаем драйвер
			char* msg = new char[3000];
			FSmartPumpControlLink = new SmartPumpControl("С:\\SmartPumpControl_Driver.dll", msg);
			if (!FSmartPumpControlLink->IsStatusOK)
				std::cout << "Ошибка подключения библиотеки";

			std::cout << "Выполняем инициализацию драйвера\n";
			//Проверяем найдена ли в драйвере функция Open и выполняем инициализацию драйвера посредством передачи в функцию Open
			//callback функций и ссылки на объект с из которого произвелась инициализация. В дальнейшем ссылка на данный объект
			//Будет передаваться обратна при вызове кажной calback функции.
			if (FSmartPumpControlLink->Open != 0)
			{
				if (FSmartPumpControlLink->Open(
					&SetDDelegate, &GetDDelegate,
					&CancelDDelegate, &HoldPDelegate,
					&UpdateFOver_Delegate, &InsertCInfo_Delegate,
					&SaveRcpt_Delegate, "Sample Control", 0) != 1)
				{
					std::cout << "Ошибка подключения библиотеки";
					std::cin >> menu;
					return 0;
				}
				else
				{
					std::cout << FSmartPumpControlLink->Description();
				}
			}
			//Проверяем найдена ли в драйвере функция FuelPrices и выполняем установку цен на топливо. Далее при каждой смене цены
			//необходимо дополнительно вызывать FuelPrices и передавать цены. При этом при изменении даже одной цены, необходимо
			//обновлять весь список цен.
			/*{
			Цены передаются в виде строки с разделителями :
			записей: ';',
			полей : '=',
			в следующем формате :

			код_продукта = наименование_вида_топлива = цена = порядковый_номер_продукта_в_системе;

			Например:
			80 = АИ - 80 = 28, 8 = 3

			80 - Код продукта
			АИ - 80 - Наименование продукта
			28, 8 - Цена одного литра продукта
			3 - Порядковый номер продукта в системе
			}
			*/
			AddText("Выполняем установку цен на топливо");
			if (FSmartPumpControlLink->FuelPrices != 0)
				FSmartPumpControlLink->FuelPrices("95=Аи-95-К5=36,2=1;92=Аи-92-К5=32,23=2;80=АИ-80=28,8=3");

			//Проверяем найдена ли в драйвере функция PumpFuels и выполняем привязку ТРК к видам топлива.
			/*{
			Соответствие  передаются в виде строки с разделителями :
			записей: ';',
			полей : '=',
			списка внутри поля : ',',
			в следующем формате :

			номер_трк = код_продукта_1, код_продукта_2, ..., код_продукта_n;

			Например:
			1 = 95, 92, 80
			1 - Номер ТРК
			95, 92, 80 - Коды продуктов доступных на данной ТРК
			}
			*/
			AddText("Выполняем привязку ТРК к видам топлива");
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
			std::cout << "Ошибка инициализации библиотеки";
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
		AddText("Налив успешно завершен");
		PriceMem = 0;
	}
}