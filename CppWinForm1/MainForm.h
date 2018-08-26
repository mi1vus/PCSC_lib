#pragma once
#include <windows.h>
#include <string>
#include <msclr/marshal_cppstd.h>
//#include <thread>
#include "Debit.h"
#include "SmartPumpControl.h"

#ifdef UNICODE
#define MessageBox  MessageBoxW
#else
#define MessageBox  MessageBoxA
#endif // !UNICODE

namespace CppWinForm1 {

	using namespace System;
	using namespace System::ComponentModel;
	using namespace System::Collections;
	using namespace System::Windows::Forms;
	using namespace System::Data;
	using namespace System::Drawing;

	typedef void(*AddText)(char* Text);
	//procedure AddText(Text: String);

	typedef bool(*DebitOperation)(_int64 TransactID, void* ctx);
	//class function DebitOperation(const TransactID : Int64;  const Ctx : Pointer) : Bool; stdcall; static;

	//Объявляем callback функции передаваемые в драйвер
	typedef _int64(*SetDoseDelegate)(_int32 Pump, _int32 Osnovan, void* ctx);
	//class function SetDoseDelegate(const Pump, Osnovan : Longint; const Ctx : Pointer) : Int64; stdcall; static;
	typedef TDispStatusInfo* (*GetDoseDelegate)(_int64 Pump, void* ctx);
	//class function GetDoseDelegate(const Pump : Int64; const Ctx : Pointer) : PDispStatusInfo; stdcall; static;
	typedef _int32(*CancelDoseDelegate)(_int64 TransID, void* ctx);
	//class function CancelDoseDelegate(const TransID : Int64; const Ctx : Pointer) : Int32; stdcall; static;
	typedef _int32(*HoldPumpDelegate)(_int32 Pump, char ReleasePump, void* ctx);
	//class function HoldPumpDelegate(const Pump : Int32; const ReleasePump : byte; const Ctx : Pointer) : Int32; stdcall; static;

	typedef _int32(*UpdateFillingOver_Delegate)(_int32 Amount,
		_int32 Price,
		_int64 Trans_ID,
		_int32 DiscountMoney,
		void* ctx);
	//class function UpdateFillingOver_Delegate(const Amount : Int32;
	//const Price : Int32;
	//const Trans_ID : Int64;
	//const DiscountMoney : Int32;
	//const Ctx : Pointer)
	//	: Int32; stdcall; static;

	typedef _int32(*InsertCardInfo_Delegate)(double _DateTime,
		char* CardNo,
		_int32 CardType,
		_int64 Trans_ID,
		void* ctx);
	//class function InsertCardInfo_Delegate(const _DateTime : TDateTime;
	//const CardNo : PAnsiChar;
	//const CardType : Int32;
	//const Trans_ID : Int64;
	//const Ctx : Pointer)
	//	: Int32; stdcall; static;

	typedef _int32(*SaveReciept_Delegate)(char* RecieptText,
		double _DateTime,
		char* DeviceName,
		char* DeviceSerial,
		_int32 DocNo,
		_int32 DocType,
		_int32 Amount,
		_int32 VarCheck,
		char* DocKind,
		_int32 DocKindCode,
		_int32 PayType,
		_int32 FactDoc,
		_int32 BP_Product,
		_int64 Trans_ID,
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
	_int64 TransCounter;

	/// <summary>
	/// Summary for MainForm
	/// </summary>
	public ref class MainForm : public System::Windows::Forms::Form
	{
	public:
		MainForm(void)
		{
			InitializeComponent();
			//
			//TODO: Add the constructor code here
			//
		}

	protected:
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		~MainForm()
		{
			if (components)
			{
				delete components;
			}
		}
	private: System::Windows::Forms::Button^  button1;
	protected:
	private: System::Windows::Forms::Button^  button2;
	private: System::Windows::Forms::Button^  button3;
	private: System::Windows::Forms::Button^  button4;
	private: System::Windows::Forms::Button^  button5;
	private: System::Windows::Forms::Button^  button6;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel1;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel2;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel3;
	private: System::Windows::Forms::GroupBox^  groupBox1;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel4;
	private: System::Windows::Forms::Button^  button7;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel8;
	private: System::Windows::Forms::Label^  label4;
	private: System::Windows::Forms::TextBox^  textBox4;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel7;
	private: System::Windows::Forms::Label^  label3;
	private: System::Windows::Forms::TextBox^  textBox3;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel6;
	private: System::Windows::Forms::Label^  label2;
	private: System::Windows::Forms::TextBox^  textBox2;
	private: System::Windows::Forms::TableLayoutPanel^  tableLayoutPanel5;
	private: System::Windows::Forms::Label^  label1;
	private: System::Windows::Forms::TextBox^  textBox1;
	private: System::Windows::Forms::TextBox^  textBox5;

	private:
		/// <summary>
		/// Required designer variable.
		/// </summary>
		System::ComponentModel::Container ^components;

#pragma region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		void InitializeComponent(void)
		{
			this->button1 = (gcnew System::Windows::Forms::Button());
			this->button2 = (gcnew System::Windows::Forms::Button());
			this->button3 = (gcnew System::Windows::Forms::Button());
			this->button4 = (gcnew System::Windows::Forms::Button());
			this->button5 = (gcnew System::Windows::Forms::Button());
			this->button6 = (gcnew System::Windows::Forms::Button());
			this->tableLayoutPanel1 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->tableLayoutPanel2 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->tableLayoutPanel3 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->groupBox1 = (gcnew System::Windows::Forms::GroupBox());
			this->tableLayoutPanel4 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->tableLayoutPanel8 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->label4 = (gcnew System::Windows::Forms::Label());
			this->textBox4 = (gcnew System::Windows::Forms::TextBox());
			this->tableLayoutPanel7 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->label3 = (gcnew System::Windows::Forms::Label());
			this->textBox3 = (gcnew System::Windows::Forms::TextBox());
			this->tableLayoutPanel6 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->label2 = (gcnew System::Windows::Forms::Label());
			this->textBox2 = (gcnew System::Windows::Forms::TextBox());
			this->button7 = (gcnew System::Windows::Forms::Button());
			this->tableLayoutPanel5 = (gcnew System::Windows::Forms::TableLayoutPanel());
			this->label1 = (gcnew System::Windows::Forms::Label());
			this->textBox1 = (gcnew System::Windows::Forms::TextBox());
			this->textBox5 = (gcnew System::Windows::Forms::TextBox());
			this->tableLayoutPanel1->SuspendLayout();
			this->tableLayoutPanel2->SuspendLayout();
			this->tableLayoutPanel3->SuspendLayout();
			this->groupBox1->SuspendLayout();
			this->tableLayoutPanel4->SuspendLayout();
			this->tableLayoutPanel8->SuspendLayout();
			this->tableLayoutPanel7->SuspendLayout();
			this->tableLayoutPanel6->SuspendLayout();
			this->tableLayoutPanel5->SuspendLayout();
			this->SuspendLayout();
			// 
			// button1
			// 
			this->button1->Anchor = System::Windows::Forms::AnchorStyles::None;
			this->button1->Location = System::Drawing::Point(26, 3);
			this->button1->Name = L"button1";
			this->button1->Size = System::Drawing::Size(236, 29);
			this->button1->TabIndex = 0;
			this->button1->Text = L"Открыть библиотеку";
			this->button1->UseVisualStyleBackColor = true;
			this->button1->Click += gcnew System::EventHandler(this, &MainForm::button1_Click);
			// 
			// button2
			// 
			this->button2->Anchor = System::Windows::Forms::AnchorStyles::None;
			this->button2->Location = System::Drawing::Point(26, 39);
			this->button2->Name = L"button2";
			this->button2->Size = System::Drawing::Size(236, 29);
			this->button2->TabIndex = 1;
			this->button2->Text = L"Закрыть библиотеку";
			this->button2->UseVisualStyleBackColor = true;
			// 
			// button3
			// 
			this->button3->Anchor = System::Windows::Forms::AnchorStyles::None;
			this->button3->Location = System::Drawing::Point(26, 75);
			this->button3->Name = L"button3";
			this->button3->Size = System::Drawing::Size(236, 29);
			this->button3->TabIndex = 2;
			this->button3->Text = L"Закрыть смену";
			this->button3->UseVisualStyleBackColor = true;
			// 
			// button4
			// 
			this->button4->Anchor = System::Windows::Forms::AnchorStyles::None;
			this->button4->Location = System::Drawing::Point(26, 111);
			this->button4->Name = L"button4";
			this->button4->Size = System::Drawing::Size(236, 29);
			this->button4->TabIndex = 3;
			this->button4->Text = L"Обслуживание";
			this->button4->UseVisualStyleBackColor = true;
			// 
			// button5
			// 
			this->button5->Anchor = System::Windows::Forms::AnchorStyles::None;
			this->button5->Location = System::Drawing::Point(26, 147);
			this->button5->Name = L"button5";
			this->button5->Size = System::Drawing::Size(236, 29);
			this->button5->TabIndex = 4;
			this->button5->Text = L"Настройка";
			this->button5->UseVisualStyleBackColor = true;
			// 
			// button6
			// 
			this->button6->Anchor = System::Windows::Forms::AnchorStyles::None;
			this->button6->Location = System::Drawing::Point(26, 186);
			this->button6->Name = L"button6";
			this->button6->Size = System::Drawing::Size(236, 29);
			this->button6->TabIndex = 5;
			this->button6->Text = L"Получить список оснований";
			this->button6->UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel1
			// 
			this->tableLayoutPanel1->ColumnCount = 1;
			this->tableLayoutPanel1->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				100)));
			this->tableLayoutPanel1->Controls->Add(this->tableLayoutPanel2, 0, 0);
			this->tableLayoutPanel1->Controls->Add(this->textBox5, 0, 1);
			this->tableLayoutPanel1->Dock = System::Windows::Forms::DockStyle::Fill;
			this->tableLayoutPanel1->Location = System::Drawing::Point(0, 0);
			this->tableLayoutPanel1->Name = L"tableLayoutPanel1";
			this->tableLayoutPanel1->RowCount = 2;
			this->tableLayoutPanel1->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Absolute, 233)));
			this->tableLayoutPanel1->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 100)));
			this->tableLayoutPanel1->Size = System::Drawing::Size(839, 635);
			this->tableLayoutPanel1->TabIndex = 6;
			// 
			// tableLayoutPanel2
			// 
			this->tableLayoutPanel2->AutoScroll = true;
			this->tableLayoutPanel2->ColumnCount = 2;
			this->tableLayoutPanel2->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				35.41077F)));
			this->tableLayoutPanel2->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				64.58923F)));
			this->tableLayoutPanel2->Controls->Add(this->tableLayoutPanel3, 0, 0);
			this->tableLayoutPanel2->Controls->Add(this->groupBox1, 1, 0);
			this->tableLayoutPanel2->Dock = System::Windows::Forms::DockStyle::Fill;
			this->tableLayoutPanel2->Location = System::Drawing::Point(3, 3);
			this->tableLayoutPanel2->Name = L"tableLayoutPanel2";
			this->tableLayoutPanel2->RowCount = 1;
			this->tableLayoutPanel2->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 100)));
			this->tableLayoutPanel2->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Absolute, 227)));
			this->tableLayoutPanel2->Size = System::Drawing::Size(833, 227);
			this->tableLayoutPanel2->TabIndex = 0;
			this->tableLayoutPanel2->Paint += gcnew System::Windows::Forms::PaintEventHandler(this, &MainForm::tableLayoutPanel2_Paint);
			// 
			// tableLayoutPanel3
			// 
			this->tableLayoutPanel3->Anchor = static_cast<System::Windows::Forms::AnchorStyles>(((System::Windows::Forms::AnchorStyles::Top | System::Windows::Forms::AnchorStyles::Bottom)
				| System::Windows::Forms::AnchorStyles::Left));
			this->tableLayoutPanel3->ColumnCount = 1;
			this->tableLayoutPanel3->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				100)));
			this->tableLayoutPanel3->Controls->Add(this->button1, 0, 0);
			this->tableLayoutPanel3->Controls->Add(this->button6, 0, 5);
			this->tableLayoutPanel3->Controls->Add(this->button2, 0, 1);
			this->tableLayoutPanel3->Controls->Add(this->button5, 0, 4);
			this->tableLayoutPanel3->Controls->Add(this->button3, 0, 2);
			this->tableLayoutPanel3->Controls->Add(this->button4, 0, 3);
			this->tableLayoutPanel3->Location = System::Drawing::Point(3, 3);
			this->tableLayoutPanel3->Name = L"tableLayoutPanel3";
			this->tableLayoutPanel3->RowCount = 6;
			this->tableLayoutPanel3->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 16.66667F)));
			this->tableLayoutPanel3->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 16.66667F)));
			this->tableLayoutPanel3->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 16.66667F)));
			this->tableLayoutPanel3->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 16.66667F)));
			this->tableLayoutPanel3->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 16.66667F)));
			this->tableLayoutPanel3->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 16.66667F)));
			this->tableLayoutPanel3->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Absolute, 20)));
			this->tableLayoutPanel3->Size = System::Drawing::Size(288, 221);
			this->tableLayoutPanel3->TabIndex = 0;
			// 
			// groupBox1
			// 
			this->groupBox1->Anchor = static_cast<System::Windows::Forms::AnchorStyles>(((System::Windows::Forms::AnchorStyles::Top | System::Windows::Forms::AnchorStyles::Left)
				| System::Windows::Forms::AnchorStyles::Right));
			this->groupBox1->Controls->Add(this->tableLayoutPanel4);
			this->groupBox1->Location = System::Drawing::Point(297, 3);
			this->groupBox1->Name = L"groupBox1";
			this->groupBox1->Size = System::Drawing::Size(533, 140);
			this->groupBox1->TabIndex = 1;
			this->groupBox1->TabStop = false;
			this->groupBox1->Text = L"ТРК 1";
			// 
			// tableLayoutPanel4
			// 
			this->tableLayoutPanel4->ColumnCount = 2;
			this->tableLayoutPanel4->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				50)));
			this->tableLayoutPanel4->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				50)));
			this->tableLayoutPanel4->Controls->Add(this->tableLayoutPanel8, 1, 1);
			this->tableLayoutPanel4->Controls->Add(this->tableLayoutPanel7, 0, 1);
			this->tableLayoutPanel4->Controls->Add(this->tableLayoutPanel6, 1, 0);
			this->tableLayoutPanel4->Controls->Add(this->button7, 1, 2);
			this->tableLayoutPanel4->Controls->Add(this->tableLayoutPanel5, 0, 0);
			this->tableLayoutPanel4->Dock = System::Windows::Forms::DockStyle::Fill;
			this->tableLayoutPanel4->Location = System::Drawing::Point(3, 18);
			this->tableLayoutPanel4->Name = L"tableLayoutPanel4";
			this->tableLayoutPanel4->RowCount = 3;
			this->tableLayoutPanel4->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 33.33333F)));
			this->tableLayoutPanel4->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 33.33333F)));
			this->tableLayoutPanel4->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 33.33333F)));
			this->tableLayoutPanel4->Size = System::Drawing::Size(527, 119);
			this->tableLayoutPanel4->TabIndex = 0;
			this->tableLayoutPanel4->Paint += gcnew System::Windows::Forms::PaintEventHandler(this, &MainForm::tableLayoutPanel4_Paint);
			// 
			// tableLayoutPanel8
			// 
			this->tableLayoutPanel8->ColumnCount = 2;
			this->tableLayoutPanel8->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				28.57143F)));
			this->tableLayoutPanel8->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				71.42857F)));
			this->tableLayoutPanel8->Controls->Add(this->label4, 0, 0);
			this->tableLayoutPanel8->Controls->Add(this->textBox4, 1, 0);
			this->tableLayoutPanel8->Dock = System::Windows::Forms::DockStyle::Fill;
			this->tableLayoutPanel8->Location = System::Drawing::Point(266, 42);
			this->tableLayoutPanel8->Name = L"tableLayoutPanel8";
			this->tableLayoutPanel8->RowCount = 1;
			this->tableLayoutPanel8->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 100)));
			this->tableLayoutPanel8->Size = System::Drawing::Size(258, 33);
			this->tableLayoutPanel8->TabIndex = 10;
			// 
			// label4
			// 
			this->label4->Anchor = System::Windows::Forms::AnchorStyles::Left;
			this->label4->AutoSize = true;
			this->label4->Location = System::Drawing::Point(3, 8);
			this->label4->Name = L"label4";
			this->label4->Size = System::Drawing::Size(50, 17);
			this->label4->TabIndex = 0;
			this->label4->Text = L"Сумма";
			// 
			// textBox4
			// 
			this->textBox4->Anchor = static_cast<System::Windows::Forms::AnchorStyles>((System::Windows::Forms::AnchorStyles::Left | System::Windows::Forms::AnchorStyles::Right));
			this->textBox4->Location = System::Drawing::Point(76, 5);
			this->textBox4->Name = L"textBox4";
			this->textBox4->Size = System::Drawing::Size(179, 22);
			this->textBox4->TabIndex = 1;
			// 
			// tableLayoutPanel7
			// 
			this->tableLayoutPanel7->ColumnCount = 2;
			this->tableLayoutPanel7->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				28.57143F)));
			this->tableLayoutPanel7->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				71.42857F)));
			this->tableLayoutPanel7->Controls->Add(this->label3, 0, 0);
			this->tableLayoutPanel7->Controls->Add(this->textBox3, 1, 0);
			this->tableLayoutPanel7->Dock = System::Windows::Forms::DockStyle::Fill;
			this->tableLayoutPanel7->Location = System::Drawing::Point(3, 42);
			this->tableLayoutPanel7->Name = L"tableLayoutPanel7";
			this->tableLayoutPanel7->RowCount = 1;
			this->tableLayoutPanel7->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 100)));
			this->tableLayoutPanel7->Size = System::Drawing::Size(257, 33);
			this->tableLayoutPanel7->TabIndex = 9;
			// 
			// label3
			// 
			this->label3->Anchor = System::Windows::Forms::AnchorStyles::Left;
			this->label3->AutoSize = true;
			this->label3->Location = System::Drawing::Point(3, 8);
			this->label3->Name = L"label3";
			this->label3->Size = System::Drawing::Size(43, 17);
			this->label3->TabIndex = 0;
			this->label3->Text = L"Цена";
			// 
			// textBox3
			// 
			this->textBox3->Anchor = static_cast<System::Windows::Forms::AnchorStyles>((System::Windows::Forms::AnchorStyles::Left | System::Windows::Forms::AnchorStyles::Right));
			this->textBox3->Location = System::Drawing::Point(76, 5);
			this->textBox3->Name = L"textBox3";
			this->textBox3->Size = System::Drawing::Size(178, 22);
			this->textBox3->TabIndex = 1;
			// 
			// tableLayoutPanel6
			// 
			this->tableLayoutPanel6->ColumnCount = 2;
			this->tableLayoutPanel6->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				28.57143F)));
			this->tableLayoutPanel6->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				71.42857F)));
			this->tableLayoutPanel6->Controls->Add(this->label2, 0, 0);
			this->tableLayoutPanel6->Controls->Add(this->textBox2, 1, 0);
			this->tableLayoutPanel6->Dock = System::Windows::Forms::DockStyle::Fill;
			this->tableLayoutPanel6->Location = System::Drawing::Point(266, 3);
			this->tableLayoutPanel6->Name = L"tableLayoutPanel6";
			this->tableLayoutPanel6->RowCount = 1;
			this->tableLayoutPanel6->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 100)));
			this->tableLayoutPanel6->Size = System::Drawing::Size(258, 33);
			this->tableLayoutPanel6->TabIndex = 8;
			// 
			// label2
			// 
			this->label2->Anchor = System::Windows::Forms::AnchorStyles::Left;
			this->label2->AutoSize = true;
			this->label2->Location = System::Drawing::Point(3, 8);
			this->label2->Name = L"label2";
			this->label2->Size = System::Drawing::Size(53, 17);
			this->label2->TabIndex = 0;
			this->label2->Text = L"Кол-во";
			// 
			// textBox2
			// 
			this->textBox2->Anchor = static_cast<System::Windows::Forms::AnchorStyles>((System::Windows::Forms::AnchorStyles::Left | System::Windows::Forms::AnchorStyles::Right));
			this->textBox2->Location = System::Drawing::Point(76, 5);
			this->textBox2->Name = L"textBox2";
			this->textBox2->Size = System::Drawing::Size(179, 22);
			this->textBox2->TabIndex = 1;
			// 
			// button7
			// 
			this->button7->Anchor = System::Windows::Forms::AnchorStyles::None;
			this->button7->Location = System::Drawing::Point(284, 84);
			this->button7->Name = L"button7";
			this->button7->Size = System::Drawing::Size(221, 29);
			this->button7->TabIndex = 6;
			this->button7->Text = L"Завершить налив";
			this->button7->UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel5
			// 
			this->tableLayoutPanel5->ColumnCount = 2;
			this->tableLayoutPanel5->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				28.57143F)));
			this->tableLayoutPanel5->ColumnStyles->Add((gcnew System::Windows::Forms::ColumnStyle(System::Windows::Forms::SizeType::Percent,
				71.42857F)));
			this->tableLayoutPanel5->Controls->Add(this->label1, 0, 0);
			this->tableLayoutPanel5->Controls->Add(this->textBox1, 1, 0);
			this->tableLayoutPanel5->Dock = System::Windows::Forms::DockStyle::Fill;
			this->tableLayoutPanel5->Location = System::Drawing::Point(3, 3);
			this->tableLayoutPanel5->Name = L"tableLayoutPanel5";
			this->tableLayoutPanel5->RowCount = 1;
			this->tableLayoutPanel5->RowStyles->Add((gcnew System::Windows::Forms::RowStyle(System::Windows::Forms::SizeType::Percent, 100)));
			this->tableLayoutPanel5->Size = System::Drawing::Size(257, 33);
			this->tableLayoutPanel5->TabIndex = 7;
			// 
			// label1
			// 
			this->label1->Anchor = System::Windows::Forms::AnchorStyles::Left;
			this->label1->AutoSize = true;
			this->label1->Location = System::Drawing::Point(3, 8);
			this->label1->Name = L"label1";
			this->label1->Size = System::Drawing::Size(58, 17);
			this->label1->TabIndex = 0;
			this->label1->Text = L"TransID";
			// 
			// textBox1
			// 
			this->textBox1->Anchor = static_cast<System::Windows::Forms::AnchorStyles>((System::Windows::Forms::AnchorStyles::Left | System::Windows::Forms::AnchorStyles::Right));
			this->textBox1->Location = System::Drawing::Point(76, 5);
			this->textBox1->Name = L"textBox1";
			this->textBox1->Size = System::Drawing::Size(178, 22);
			this->textBox1->TabIndex = 1;
			// 
			// textBox5
			// 
			this->textBox5->Dock = System::Windows::Forms::DockStyle::Fill;
			this->textBox5->Location = System::Drawing::Point(3, 236);
			this->textBox5->Multiline = true;
			this->textBox5->Name = L"textBox5";
			this->textBox5->ScrollBars = System::Windows::Forms::ScrollBars::Both;
			this->textBox5->Size = System::Drawing::Size(833, 396);
			this->textBox5->TabIndex = 1;
			// 
			// MainForm
			// 
			this->AutoScaleDimensions = System::Drawing::SizeF(8, 16);
			this->AutoScaleMode = System::Windows::Forms::AutoScaleMode::Font;
			this->ClientSize = System::Drawing::Size(839, 635);
			this->Controls->Add(this->tableLayoutPanel1);
			this->Name = L"MainForm";
			this->Text = L"MainForm";
			this->tableLayoutPanel1->ResumeLayout(false);
			this->tableLayoutPanel1->PerformLayout();
			this->tableLayoutPanel2->ResumeLayout(false);
			this->tableLayoutPanel3->ResumeLayout(false);
			this->groupBox1->ResumeLayout(false);
			this->tableLayoutPanel4->ResumeLayout(false);
			this->tableLayoutPanel8->ResumeLayout(false);
			this->tableLayoutPanel8->PerformLayout();
			this->tableLayoutPanel7->ResumeLayout(false);
			this->tableLayoutPanel7->PerformLayout();
			this->tableLayoutPanel6->ResumeLayout(false);
			this->tableLayoutPanel6->PerformLayout();
			this->tableLayoutPanel5->ResumeLayout(false);
			this->tableLayoutPanel5->PerformLayout();
			this->ResumeLayout(false);

		}
#pragma endregion
private: System::Void tableLayoutPanel4_Paint(System::Object^  sender, System::Windows::Forms::PaintEventArgs^  e) {
}
private: System::Void tableLayoutPanel2_Paint(System::Object^  sender, System::Windows::Forms::PaintEventArgs^  e) {
}

public: 
bool DebOperation(_int64 TransactID, void* ctx)
{
		 //ATrans: PTransactionInfo;
		 //FTrans: TTransactionInfo;
			// _Price, _Quantity, _Amount : Double;
		 //orderMode:String;


		 //ATrans: = @FTrans;
			// TSDIAppForm(Ctx).AddText(pchar('Получение ниформации о заказе, TransactID: ' + IntToStr(TransactID)));
			//	 if FSmartPumpControlLink.GetTransactionFunc(TransactID, ATrans) = 1
			//		 then begin
			//		 if ATrans.OrderInMoney = 1 then
			//			 begin
			//			 orderMode : = 'Денежный заказ';
			//	 end else begin
			//		 orderMode : = 'Литровый заказ';
			//	 end;
			// _Price: = ATrans.Price / 100;
			// _Quantity: = ATrans.Quantity / 1000;
			// _Amount: = ATrans.Amount / 100;

			//	 TSDIAppForm(Ctx).AddText(pchar('ТРК:            ' + IntToStr(ATrans.Pump)
			//		 + #13#10 + 'Основание:      ' + IntToStr(ATrans.PaymentCode)
			//		 + #13#10 + 'Продукт:        ' + IntToStr(ATrans.Fuel)
			//		 + #13#10 + 'Режим заказа:   ' + orderMode
			//		 + #13#10 + 'Количество:     ' + FloatToStr(_Quantity)
			//		 + #13#10 + 'Цена:           ' + FloatToStr(_Price)
			//		 + #13#10 + 'Сумма:          ' + FloatToStr(_Amount)
			//		 + #13#10 + 'Номер карты:    ' + ATrans.CardNum
			//		 + #13#10 + 'RRN Транзакции: ' + ATrans.RRN) + #13#10 + #13#10);

			//	 TSDIAppForm(Ctx).AmountMem : = ATrans.Amount;
			//	 TSDIAppForm(Ctx).VolumeMem : = ATrans.Quantity;
			//	 TSDIAppForm(Ctx).PriceMem : = ATrans.Price;
			//	 TSDIAppForm(Ctx).TransID_Edit.Text : = IntToStr(TransactID);
			//	 TSDIAppForm(Ctx).Price.Text : = FloatToStrF(_Price, ffFixed, 100, 2);
			//	 TSDIAppForm(Ctx).Volume.Text : = FloatToStrF(_Quantity, ffFixed, 100, 2);
			//	 TSDIAppForm(Ctx).Amount.Text : = FloatToStrF(_Amount, ffFixed, 100, 2);
			//	 TSDIAppForm(Ctx).FillingOver.Enabled : = true;
			//	 end;
}

//Open dll
private: System::Void button1_Click(System::Object^  sender, System::EventArgs^  e) 
{
	try
	{
		DebitTh = new DebitThread();
		DebitTh->debitCallback = &DebOperation;
		DebitTh->ctx = nullptr;
		//std::thread t1(DebitThread::Execute);

		// wait for return
		//t1.join();
		textBox5->Text += "Загружаем драйвер\n";

/*		//Загружаем драйвер
		if (!LinkSmartPumpControl(ExtractFilePath(GetModuleName(HInstance)) + SmartPumpControl_driver_dll, @FSmartPumpControlLink, @_Msg))
			auto msgboxID = MessageBoxW(nullptr, L"Ошибка подключения библиотеки", L"SmartPumpControl", MB_OK + MB_ICONINFORMATION);

		textBox5->Text += "Выполняем инициализацию драйвера\n";
		//Проверяем найдена ли в драйвере функция Open и выполняем инициализацию драйвера посредством передачи в функцию Open
		//callback функций и ссылки на объект с из которого произвелась инициализация. В дальнейшем ссылка на данный объект
		//Будет передаваться обратна при вызове кажной calback функции.
		if Assigned(FSmartPumpControlLink.Open) then begin
			if FSmartPumpControlLink.Open(SetDoseDelegate, GetDoseDelegate,
				CancelDoseDelegate, HoldPumpDelegate,
				UpdateFillingOver_Delegate, InsertCardInfo_Delegate,
				SaveReciept_Delegate, PAnsiChar(AnsiString('Sample Control')), Self) < > 1 then begin
				Windows.MessageBox(0, 'Ошибка подключения библиотеки', 'SmartPumpControl', MB_OK + MB_ICONINFORMATION);
		Exit;
		end
			else begin
			AddText(FSmartPumpControlLink.Description);
		end;
		end;
		//Проверяем найдена ли в драйвере функция FuelPrices и выполняем установку цен на топливо. Далее при каждой смене цены
		//необходимо дополнительно вызывать FuelPrices и передавать цены. При этом при изменении даже одной цены, необходимо
		//обновлять весь список цен.
		{
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
		AddText('Выполняем установку цен на топливо');
		if Assigned(FSmartPumpControlLink.FuelPrices)  then begin
			FSmartPumpControlLink.FuelPrices('95=Аи-95-К5=36,2=1;92=Аи-92-К5=32,23=2;80=АИ-80=28,8=3')
			end;
		//Проверяем найдена ли в драйвере функция PumpFuels и выполняем привязку ТРК к видам топлива.
		{
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
		AddText('Выполняем привязку ТРК к видам топлива');
		if Assigned(FSmartPumpControlLink.PumpFuels)  then begin
			FSmartPumpControlLink.PumpFuels('1=95,92,80;2=95,92,80;3=95,92;4=95,92')
			end;
		Open.Enabled : = False;
		Close.Enabled : = True;
		CloseShift.Enabled : = True;
		Service.Enabled : = True;
		Settings.Enabled : = True;
		GetCardTypes.Enabled : = True;
		*/
	}
	catch(Exception^ ex)
	{
		std::string msg = msclr::interop::marshal_as<std::string>(ex->ToString());
		auto msgboxID = MessageBox(nullptr, L"Ошибка инициализации библиотеки", (LPCWSTR)msg.c_str(), MB_OK);
	}
}

	
};
}
