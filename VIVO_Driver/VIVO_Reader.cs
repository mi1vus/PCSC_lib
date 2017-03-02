using System;
using System.IO;
using System.Threading;
using UsbLibrary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoverConstants;
using MarshalHelper;

namespace VIVO_Driver
{
    public static class VIVOReader
    {
        #region Переменные

        public static UsbHidPort USB = new UsbHidPort();
        public static ushort USBDevProduct;
        public static ushort USBDevVendor;
        public static int USBDevIRL;
        public static int USBDevORL;
        public static int USBDevFRL;

        private static IntPtr _obj = IntPtr.Zero;
        private static readonly object LockObj = new object();
        public static string LogirPath = @"C:\ServioCardAPI\SDK\Out";
        public static CardType CardType;
        public static byte[] CardId;

        private static readonly byte[] InitArray
            = { 0x56, 0x69, 0x56, 0x4f, 0x74, 0x65, 0x63, 0x68, 0x32, 0x00 };

        private static readonly AutoResetEvent ResponseReceived = new AutoResetEvent(false);
        //public static bool isResponseReceived;
        public static byte[] LastCommandData;
        public static byte[] LastResponseData;
        private static byte[] _tempData;

        public static byte LastCommandCode;
        public static byte LastSubCommandCode;
        public static Status LastCommandStatus = Status.Empty;
        public static bool LastResponseCRCIsOK; 
        private static bool _waitNextResponse;

        #endregion

        #region Интерфейс библиотеки
        //--------------------------sector,KeyType ---> slot, nonvolatile, key (6 bite)
        private static Dictionary<Tuple<int, int>, Tuple<int, bool, byte[]>> _keys;
        private static HashSet<Tuple<int, int>> _cardBadSectors;

        // Все функции возвращают код ошибки
        // Код 0 - нет ошибок. Другие значения используются для обозначения
        // ошибочных ситуаций, которые приведут к прерыванию транзакции

        // Получить описание ошибки (const ErrorCode : Integer; const DescriptionBuf : PWideChar; const BufLen : Integer; const Obj : Pointer) : Integer; stdcall;
        //   ErrorCode - код ошибки
        //   DescriptionBuf - выходной буфер
        //   BufLen - длина выходного буфера в символах
        //   Obj - ссылка на объект считывателя (если используется)
        // * Вызывающая сторона может потребовать описание до того как считыватель
        //   инициализирован или если произошла ошибка инициализации
        [Obfuscation]
        public static int GetErrorDescription(int errorCode, [MarshalAs(UnmanagedType.LPWStr)] ref string descriptionBuf,
            int bufLen, IntPtr obj)
        {
            string text =
                $"!!! GetErrorDescription !!!\tobj:{obj}\terr:{errorCode}\tDescriptionBuf:{descriptionBuf}\tbufLen:{bufLen}\t";
            WriteToLog(text, false);
            try
            {
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"GetErrorDescription ERROR !!! {text}\r\n {e}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Получить список логических устройств (const ItemsBuf : PWideChar; const BufLen : Integer; const Obj : Pointer) : Integer; stdcall;
        //   ItemsBuf - выходной буфер. Названия логических устройств разделены символом #0
        //   BufLen - длина выходного буфера в символах
        [Obfuscation]
        public static int GetLogicalDevices([MarshalAs(UnmanagedType.LPWStr)] ref string itemsBuf, ref int bufLen,
            IntPtr obj)
        {
            string text =
                $"!!! GetLogicalDevices !!!\tobj:{obj}\tItemsBuf:{itemsBuf}\tbufLen:{bufLen}\t";
            WriteToLog(text, false);
            try
            {
                var readers = new[] { "VIVOPay" };//CardReader.GetReaderNames().ToArray();
                string res = "";
                Array.ForEach(readers, t => res += (string.IsNullOrWhiteSpace(res) ? "" : "#0") + t);
                res += '\0';
                bufLen = readers.Length;
                text = $"!!! GetLogicalDevices !!!\tLogicalDevices:{res}";
                WriteToLog(text, false);

                //UnMemory<char>.SaveInMemArr(res.ToCharArray(), ref itemsBuf);
                itemsBuf = res;

                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"GetLogicalDevices ERROR !!! {text}\r\n {e}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        [Obfuscation]
        public static string InitReader(string reader, string log)
        {
            if (!string.IsNullOrWhiteSpace(LogirPath))
                WriteToLog("InitReader", false);

            if (USBDevProduct != 0 && USBDevVendor != 0)
                return "PID/VID is set";

            if (/*string.IsNullOrWhiteSpace(reader) || */string.IsNullOrWhiteSpace(log))
                return "null log";

            //var pidVid = reader.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries);
            //USBDevProduct = ushort.Parse(pidVid[0], NumberStyles.HexNumber);
            //USBDevVendor = ushort.Parse(pidVid[1], NumberStyles.HexNumber);
            USBDevProduct = 0x0100;
            USBDevVendor = 0x1D5F;

            LogirPath = log;
            string result = $"reader init is: {USBDevProduct}_{USBDevVendor} \r\n\t\tlog:{LogirPath}";
            WriteToLog(result, false);
            return "success";
        }

        // Инициализация считывателя (const InitStr : PWideChar; const Caps : PMifareClassicReaderCaps; const Obj : PPointer) : Integer; stdcall;
        //   InitParams - параметры инициализации. Param1=Value1;Param2=Value2...
        //   Caps - возможности считывателя
        //   Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int Init([MarshalAs(UnmanagedType.LPWStr)] string /*IntPtr*/ initStr, IntPtr caps, ref IntPtr obj)
        {
            string text =
                $"!!! Init !!!\tobj:{obj}\tCaps:{caps}\tInitStr:{initStr/*Marshal.PtrToStringBSTR(initStr)*/ ?? "null"}\t pid {USBDevProduct}\t vid {USBDevVendor}";
            WriteToLog(text);
            try
            {
                UnMemory<int>.SaveInMemArr(new[] { 12, 1, 0 }, ref caps);

                var capsR = UnMemory<int>.ReadInMemArr(caps, 3);

                text =
                    $"!!! Init internal Caps !!!\tsize:{capsR[0]}\tVolatileKeySlotCount:{capsR[1]}\tNonvolatileKeySlotCount:{capsR[2]}\t";
                WriteToLog(text);

                if (!USB.Ready())
                {
                    Init();

                    if (USB.Ready())
                    {
                        string usbDevVendorS = null;
                        string usbDevProductS = null;
                        USB.GetInfoStrings(ref usbDevVendorS, ref usbDevProductS);

                        USBDevIRL = USB.SpecifiedDevice.InputReportLength;
                        USBDevORL = USB.SpecifiedDevice.OutputReportLength;
                        USBDevFRL = USB.SpecifiedDevice.FeatureReportLength;
                        string result = $"reader set is:\n{usbDevProductS}\n{usbDevVendorS}";
                        WriteToLog(result, false);

                    }
                    // выделение  памяти под obj
                    int pInt = 0;
                    UnMemory<int>.SaveInMem(pInt, ref obj);
                    _obj = obj;

                    if (_keys == null)
                        _keys = new Dictionary<Tuple<int, int>, Tuple<int, bool, byte[]>>();
                    else
                        _keys.Clear();
                }
                //WriteToLog($"Init ready:\t {USB.Ready()}", false);
                //return (int) ErrorCodes.E_SUCCESS;
                return (USB.Ready() ? (int)ErrorCodes.E_SUCCESS : (int)ErrorCodes.E_CARDREADER_NOT_INIT);
            }
            catch (Exception e)
            {
                WriteToLog($"Init ERROR !!! reader:\t pid {USBDevProduct}\t vid {USBDevVendor}\r\n {e}", true);
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
        }

        // Деинициализация считывателя (IntPtr Obj);
        // Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int Deinit(IntPtr obj)
        {
            string text =
                $"!!! Deinit !!!\tobj:{obj}\t";
            WriteToLog(text, false);
            try
            {
                _keys.Clear();

                // очищаем память для выделенных объектов
                //UnMemory.FreeMemory();
                Marshal.FreeCoTaskMem(_obj);

                if (USB.Ready())
                {
                    USB.Close();

                    USBDevVendor = USBDevProduct = 0;
                    USBDevIRL = USBDevORL = USBDevFRL = 0;
                }
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"Deinit ERROR !!! {text}\r\n {e}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Загрузка ключей. Может происходить сразу после инициализации считывателя (IntPtr Obj,; const Sector, KeyType : Integer; const NonvolatileMemory : LongBool; const KeyIndex : Integer; const Key : Pointer) : Integer; stdcall;
        // либо после подключения к карте.
        //   Obj - ссылка на объект считывателя
        //   Sector - номер сектора (0..47)
        //   KeyType - тип ключа (KEY_TYPE_A/KEY_TYPE_B)
        //   NonvolatileMemory - загрузить ключ в энергонезависимое хранилище (EEPROM)
        //   KeyIndex - номер ключа (KEY_SLOT_0 + n)
        //   Key - данные ключа, 6 байт
        [Obfuscation]
        public static int LoadKey(IntPtr obj, int sector, int keyTypeInt, bool nonvolatileMemory, int keyIndex, IntPtr key)
        {
            string text =
                $"!!! LoadKey !!!\tobj:{obj}\tSector:{sector}\tKeyType:{keyTypeInt}\tNonvolatileMemory:{nonvolatileMemory}\tKeyIndex:{keyIndex}\tKey:{key}\t";
            //WriteToLog(text);
            try
            {
                if (_keys != null)
                {
                    byte[] keyR = UnMemory<byte>.ReadInMemArr(key, 6);
                    _keys[new Tuple<int, int>(sector, keyTypeInt)] = new Tuple<int, bool, byte[]>(keyIndex, nonvolatileMemory, keyR);

                    text =
                        $"!!! LoadKey over !!!\tSector:{sector}\tKeyType:{keyTypeInt}\tKeyIndex:{keyIndex}\tkey:{BitConverter.ToString(keyR ?? new byte[] { })}\t";
                    //WriteToLog(text);
                }
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"LoadKey ERROR !!! {text}\r\n {e}", true);
                return (int)ErrorCodes.E_POS_KEYS_LOAD;
            }
        }

        // Поиск стандартных меток
        // Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int RequestStandard(IntPtr obj)
        {
            string text =
                $"!!! RequestStandard !!!\tobj:{obj}\t";
            WriteToLog(text);
            try
            {
                if (SendCmd(Commands.Ping, new byte[] { }) == Status.OK)
                {
                    if (SendCmd(Commands.Pass, new byte[] { 0x1 }) == Status.OK)
                    {
                        if (SendCmd(Commands.Poll, new byte[] {0x0, 0xC8}) == Status.OK) //2sec timeout
                            return (int) ErrorCodes.E_SUCCESS;
                        else
                        {
                            WriteToLog($"ERROR Poll {LastCommandStatus}", true);
                            return (int)ErrorCodes.E_WAIT_TIMEOUT;
                        }
                    }
                    else
                    {
                        WriteToLog($"ERROR Pass {LastCommandStatus}", true);
                        return (int)ErrorCodes.E_GENERIC;
                    }
                }
                else
                {
                    WriteToLog($"ERROR Ping {LastCommandStatus}", true);
                    return (int)ErrorCodes.E_GENERIC;
                }
            }
            catch (Exception e)
            {
                WriteToLog($"RequestStandard ERROR !!! {text}\r\n {e}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Поиск всех меток
        // Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int RequestAll(IntPtr obj)
        {
            string text =
                $"!!! RequestAll !!!\tobj:{obj}\t";
            WriteToLog(text);
            try
            {
                if (SendCmd(Commands.Ping, new byte[] { }) == Status.OK)
                {
                    if (SendCmd(Commands.Pass, new byte[] { 0x1 }) == Status.OK)
                    {
                        if (SendCmd(Commands.Poll, new byte[] { 0x0, 0xC8 }) == Status.OK) //2sec timeout
                            return (int)ErrorCodes.E_SUCCESS;
                        else
                        {
                            WriteToLog($"ERROR Poll {LastCommandStatus}", true);
                            return (int)ErrorCodes.E_WAIT_TIMEOUT;
                        }
                    }
                    else
                    {
                        WriteToLog($"ERROR Pass {LastCommandStatus}", true);
                        return (int)ErrorCodes.E_GENERIC;
                    }
                }
                else
                {
                    WriteToLog($"ERROR Ping {LastCommandStatus}", true);
                    return (int)ErrorCodes.E_GENERIC;
                }
            }
            catch (Exception e)
            {
                WriteToLog($"RequestAll ERROR !!! {text}\r\n {e.Message}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Получение номера какой-либо метки (const Obj : Pointer; const SerialNumberBuf : Pointer; const BufSize : Integer; const SerialNumberSize : PInteger) : Integer;
        //   Obj - ссылка на объект считывателя
        //   SerialNumberBuf - выходной буфер для аппаратного номера карты
        //   BufSize - размер выходного буфера
        //   SerialNumberSize - размер считанного аппаратного номера карты в байтах (обычно 4 или 7)
        [Obfuscation]
        public static int Anticollision(IntPtr obj, IntPtr serialNumberBuf, int bufSize, IntPtr serialNumberSize)
        {
            string text =
                $"!!! Anticollision !!!\tobj:{obj}\tSerialNumberBuf:{serialNumberBuf}\tBufSize:{bufSize}\tSerialNumberSize:{serialNumberSize}\t";
            WriteToLog(text);

            try
            {
                var uid = CardId;
                if (uid != null && uid.Length > 0)
                {
                    byte[] truncUid = new byte[Math.Min(bufSize, uid.Length)];
                    Array.Copy(uid, truncUid, truncUid.Length);
                    UnMemory<int>.SaveInMem(truncUid.Length, ref serialNumberSize);
                    UnMemory<byte>.SaveInMemArr(uid, ref serialNumberBuf);
                    //Marshal.StructureToPtr(memory_object, SerialNumberBuf, true);

                    var uidWrited = UnMemory<byte>.ReadInMemArr(serialNumberBuf, truncUid.Length);
                    var serialNumberSizeWrited = UnMemory<int>.ReadInMem(serialNumberSize);

                    text =
                        $"!!! Anticollision over !!!\tobj:{obj}\tSerialNumberBuf:{serialNumberBuf}\tBufSize:{bufSize}\tuid:{BitConverter.ToString(uid)}\tuid_w:{BitConverter.ToString(uidWrited ?? new byte[] { })}\tSerialNumberSize:{serialNumberSize}\tSerialNumberSize_writed:{serialNumberSizeWrited}\t";
                    WriteToLog(text);
                    return (int)ErrorCodes.E_SUCCESS;
                }

                WriteToLog($"Anticollision ERROR !!! uid:{uid == null}", true);
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Anticollision ERROR !!! {text}\r\n {e}");
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Выбор метки по номеру
        //   Obj - ссылка на объект считывателя
        //   SerialNumber - серийный номер метки
        //   SerialNumberSize - размер серийного номера метки в байтах
        [Obfuscation]
        public static int SelectCard(IntPtr obj, IntPtr serialNumber, int serialNumberSize)
        {
            string text =
                $"!!! SelectCard !!!\tobj:{obj}\tSerialNumber:{serialNumber}\tSerialNumberSize:{serialNumberSize}\t";
            WriteToLog(text);
            try
            {
                byte[] uid = UnMemory<byte>.ReadInMemArr(serialNumber, serialNumberSize);

                text =
                    $"!!! SelectCard over !!!\tobj:{obj}\tSerialNumber:{serialNumber}\tSerialNumberSize:{serialNumberSize}\tuid:{BitConverter.ToString(uid ?? new byte[] { })}\t";
                WriteToLog(text);
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"SelectCard ERROR !!! {text}\r\n {e}");
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Аутентификация в секторе
        //   Obj - ссылка на объект считывателя
        //   Sector - номер сектора
        //   KeyType - тип ключа (KEY_TYPE_A/KEY_TYPE_B)
        //   NonvolatileMemory - аутентификация ключа из энергонезависимой памяти
        //   KeyIndex - номер ключа (KEY_SLOT_0 + n)
        [Obfuscation]
        public static int Authentication(IntPtr obj, int sector, int keyTypeInt, bool nonvolatileMemory, int keyIndex)
        {
            string text =
                $"!!! Authentication !!!\tobj:{obj}\tSector:{sector}\tKeyType:{keyTypeInt}\tNonvolatileMemory:{nonvolatileMemory}\tKeyIndex:{keyIndex}\t";
            WriteToLog(text);
            try
            {
                if (!USB.Ready())
                {
                    WriteToLog($"!!! Authentication ERROR _reader");
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
                }
                var keyA = GetKeyFromCollection(sector, 0);
                var keyB = GetKeyFromCollection(sector, 1);

                byte block = SectorToBlock(sector);
                Status tmpRes = Status.OK;
                if (/*KeyType_int == 0 &&*/ keyA != null)
                {
                    KeyType keyT = KeyType.KeyA;

                    var data = new byte[keyA.Length + 2];
                    data[0] = block;
                    data[1] = (byte)keyT;
                    Array.Copy(keyA, 0, data, 2, keyA.Length);

                    tmpRes = SendCmd(Commands.Auth, data);
                }
                if (/*KeyType_int == 1 &&*/ keyB != null && tmpRes == Status.OK)
                {
                    KeyType keyT = KeyType.KeyB;

                    var data = new byte[keyB.Length + 2];
                    data[0] = block;
                    data[1] = (byte)keyT;
                    Array.Copy(keyB, 0, data, 2, keyB.Length);

                    tmpRes = SendCmd(Commands.Auth, data);
                }
/*
                var sec = _card.GetSector(Sector);

                //TODO только для тестов!
                if (_cardBadSectors?.Contains(new Tuple<int, int>(Sector, sec.NumDataBlocks -  1))??false)
                    return (int) ErrorCodes.E_SUCCESS;

                var secAuthentification = sec.GetData(0).Result;
                if (secAuthentification == null)
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
*/
                return tmpRes == Status.OK? (int)ErrorCodes.E_SUCCESS: (int)ErrorCodes.E_POS_KEYS_LOAD;
            }
            catch (Exception e)
            {
                WriteToLog($"Authentication ERROR !!! reader: {e}");
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
        }

        // Считать блок (const Obj : Pointer; const Block : Integer; const Buffer : Pointer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Buffer - буфер карты, 16 байт
        [Obfuscation]
        public static int ReadBlock(IntPtr obj, int block, IntPtr buffer)
        {
            string text =
                $"!!! ReadBlock !!!\tobj:{obj}\tBlock:{block}\tBuffer:{buffer}\t";
            WriteToLog(text);
            try
            {
                if (!USB.Ready())
                {
                    WriteToLog($"!!! ReadBlock ERROR _reader");
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
                }

                int blockInSector;
                int controlSector;
                BlockToSectorBlock(block, out controlSector, out blockInSector);

                byte blocks = 1;
                var lBitType = ((int)CardType << 4) + blocks;//read n blacks (max = 15)
                byte card_blocks = BitConverter.GetBytes(lBitType)[0];

                var keyA = GetKeyFromCollection(controlSector, 0);
                var keyB = GetKeyFromCollection(controlSector, 1);

                text =
                    $"!!! ReadBlock before !!!\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                WriteToLog(text);

                //TODO удалить
                if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                {
                    byte[] dataBadSectors = ReadFromFileSector(controlSector, blockInSector);

                    if (dataBadSectors.Length > 0 && dataBadSectors.Length < 17)
                    {
                        WriteToLog($"SaveInMemory from file {dataBadSectors.Length} byte");
                        UnMemory<byte>.SaveInMemArr(dataBadSectors, ref buffer);
                    }

                    //TODO убрать логи чтения и записи
                    //WriteToLog($"Sector '{controlSector}':[{blockInSector}]{dataBadSectors.ByteArrayToString()}");
                    //WriteDataToLog(controlSector, blockInSector, dataBadSectors.ByteArrayToString(), false);
                    return (int)ErrorCodes.E_SUCCESS;
                }

                Status tmpRes = Status.OK;
                if (/*KeyType_int == 0 &&*/ keyA != null)
                {
                    KeyType keyT = KeyType.KeyA;

                    var data = new byte[keyA.Length + 2];
                    data[0] = (byte)block;
                    data[1] = (byte)keyT;
                    Array.Copy(keyA, 0, data, 2, keyA.Length);

                    tmpRes = SendCmd(Commands.Poll, new byte[] {0x0, 0xC8}); //2sec timeout
                    if (tmpRes == Status.OK)
                        tmpRes = SendCmd(Commands.Auth, data);
                    if (tmpRes == Status.OK)
                        tmpRes = SendCmd(Commands.Read, new byte[] { card_blocks, (byte)block });
                }
                if (/*KeyType_int == 1 &&*/ keyB != null && tmpRes == Status.OK)
                {
                    KeyType keyT = KeyType.KeyB;

                    var data = new byte[keyB.Length + 2];
                    data[0] = (byte)block;
                    data[1] = (byte)keyT;
                    Array.Copy(keyB, 0, data, 2, keyB.Length);

                    tmpRes = SendCmd(Commands.Poll, new byte[] { 0x0, 0xC8 }); //2sec timeout
                    if (tmpRes == Status.OK)
                        tmpRes = SendCmd(Commands.Auth, data);
                    if (tmpRes == Status.OK)
                        tmpRes = SendCmd(Commands.Read, new byte[] { card_blocks, (byte)block });
                }
                /*
                                var sec = _card.GetSector(Sector);

                                //TODO только для тестов!
                                if (_cardBadSectors?.Contains(new Tuple<int, int>(Sector, sec.NumDataBlocks -  1))??false)
                                    return (int) ErrorCodes.E_SUCCESS;

                                var secAuthentification = sec.GetData(0).Result;
                                if (secAuthentification == null)
                                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
                */

                if (tmpRes == Status.OK && LastResponseData.Length > 0 && LastResponseData.Length < 17)
                {
                    WriteToLog($"SaveInMemory {LastResponseData.Length} byte");
                    UnMemory<byte>.SaveInMemArr(LastResponseData, ref buffer);

                    //TODO убрать логи чтения и записи
                    //WriteToLog($"Sector '{controlSector}':[{blockInSector}]{data.ByteArrayToString()}");
                    //WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), false);

                    return (int)ErrorCodes.E_SUCCESS;
                }
                return (int)ErrorCodes.E_GENERIC;
                //WriteToLog($"ReadBlock ERROR !!! reader:{_reader} card: {_card}\r\n");
                //return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
                //return tmpRes == Status.OK ? (int)ErrorCodes.E_SUCCESS : (int)ErrorCodes.E_POS_KEYS_LOAD;
            }
            catch (Exception e)
            {
                WriteToLog($"ReadBlock ERROR !!! {text}\r\n {e}");
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Записать блок (const Obj : Pointer; const Block : Integer; const Data : Pointer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Data - данные блока для записи (16 байт)
        [Obfuscation]
        public static int WriteBlock(IntPtr obj, int block, IntPtr buffer)
        {
            string text =
                $"!!! WriteBlock !!!\tobj:{obj}\tBlock:{block}\tData:{buffer}\t";

            WriteToLog(text);
            try
            {
                if (!USB.Ready())
                {
                    WriteToLog($"!!! WriteBlock ERROR _reader");
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
                }

                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(block, out controlSector, out blockInSector);

                    byte blocks = 1;
                    var lBitType = ((int)CardType << 4) + blocks;//read n blacks (max = 15)
                    byte card_blocks = BitConverter.GetBytes(lBitType)[0];

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    byte[] toWrite = UnMemory<byte>.ReadInMemArr(buffer, 16);

                    var dataWrite = new byte[toWrite.Length + 2];
                    dataWrite[0] = card_blocks;
                    dataWrite[1] = (byte)block;
                    Array.Copy(toWrite, 0, dataWrite, 2, toWrite.Length);

                    text = $"!!! WriteBlock before !!!\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    //TODO удалить
                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                    {
                        byte[] dataBadSectors = ReadFromFileSector(controlSector, blockInSector);

                        if (dataBadSectors.Length > 0 && dataBadSectors.Length < 17)
                        {
                            WriteToLog($"SaveInMemory from file {dataBadSectors.Length} byte");
                            UnMemory<byte>.SaveInMemArr(dataBadSectors, ref buffer);
                        }

                        //TODO убрать логи чтения и записи
                        //WriteToLog($"Sector '{controlSector}':[{blockInSector}]{dataBadSectors.ByteArrayToString()}");
                        //WriteDataToLog(controlSector, blockInSector, dataBadSectors.ByteArrayToString(), false);
                        return (int)ErrorCodes.E_SUCCESS;
                    }

                    Status tmpRes = Status.OK;
                    if (/*KeyType_int == 0 &&*/ keyA != null)
                    {
                        KeyType keyT = KeyType.KeyA;

                        var dataAut = new byte[keyA.Length + 2];
                        dataAut[0] = (byte)block;
                        dataAut[1] = (byte)keyT;
                        Array.Copy(keyA, 0, dataAut, 2, keyA.Length);

                        tmpRes = SendCmd(Commands.Poll, new byte[] { 0x0, 0xC8 }); //2sec timeout
                        if (tmpRes == Status.OK)
                            tmpRes = SendCmd(Commands.Auth, dataAut);
                        if (tmpRes == Status.OK)
                            tmpRes = SendCmd(Commands.Write, dataWrite);
                    }
                    if (/*KeyType_int == 1 &&*/ keyB != null && tmpRes == Status.OK)
                    {
                        KeyType keyT = KeyType.KeyB;

                        var dataAut = new byte[keyB.Length + 2];
                        dataAut[0] = (byte)block;
                        dataAut[1] = (byte)keyT;
                        Array.Copy(keyB, 0, dataAut, 2, keyB.Length);

                        tmpRes = SendCmd(Commands.Poll, new byte[] { 0x0, 0xC8 }); //2sec timeout
                        if (tmpRes == Status.OK)
                            tmpRes = SendCmd(Commands.Auth, dataAut);
                        if (tmpRes == Status.OK)
                            tmpRes = SendCmd(Commands.Write, dataWrite);
                    }

                    text =
                        $"!!! WriteBlock internal !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}\tData:{BitConverter.ToString(toWrite ?? new byte[] { })}\t";
                    WriteToLog(text);

                    //TODO убрать
                    //if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                    //{
                    //    WriteOrReplaceToFileSector(controlSector, blockInSector, dat);
                    //    text = "!!! WriteBlock to file!";
                    //    //WriteToLog(text);
                    //    return (int)ErrorCodes.E_SUCCESS;
                    //}

                    //WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), true);
                    if (tmpRes == Status.OK)
                    { 
                        return (int)ErrorCodes.E_SUCCESS;
                    }
                    return (int)ErrorCodes.E_GENERIC;
                }
                //WriteToLog($"WriteBlock ERROR !!! reader:{_reader} card: {_card}\r\n");
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"WriteBlock ERROR !!! {text}\r\n {e}");
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Уменьшить значение Value-блока (const Obj : Pointer; const Block : Integer; const Value : Integer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Value - Значение, на которое будет уменьшен счетчик блока
        [Obfuscation]
        public static int Decrement(IntPtr obj, int block, int value)
        {
            throw new NotImplementedException();
            string text =
                $"!!! Decrement !!!\tobj:{obj}\tBlock:{block}\tValue:{value}\t";
            WriteToLog(text);
            try
            {
                if (!USB.Ready())
                {
                    WriteToLog($"!!! WriteBlock ERROR _reader");
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
                }

                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(block, out controlSector, out blockInSector);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! Decrement before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                    {
                        byte[] dataBadSectors = ReadFromFileSector(controlSector, blockInSector);
                        IncDecByteArray(ref dataBadSectors, -value);
                        WriteOrReplaceToFileSector(controlSector, blockInSector, dataBadSectors);
                        //TODO убрать логи чтения и записи
                        //WriteToLog($"Decremented sector in file '{controlSector}':[{blockInSector}]{dataBadSectors.ByteArrayToString()}");
                        //WriteDataToLog(controlSector, blockInSector, dataBadSectors.ByteArrayToString(), true);

                        return (int)ErrorCodes.E_SUCCESS;
                    }

                    //byte[] data = GetData(controlSector, blockInSector, keyA, keyB).Result;
                    //IncDecByteArray(ref data, -value);
                    //var result = SetData(data, controlSector, blockInSector, keyA, keyB).Result;

                    //if (result == (int)ErrorCodes.E_SUCCESS)
                    //    result = Transfer(obj, block);

                    ////TODO убрать логи чтения и записи
                    ////WriteToLog($"Decremented sector '{controlSector}':[{blockInSector}]{data.ByteArrayToString()}");
                    ////WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), true);

                    //return result;
                }
                //WriteToLog($"Decrement ERROR !!! reader:{_reader} card: {_card}\r\n");
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Decrement ERROR !!! {text}\r\n {e}");
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Увеличить значение Value-блока (const Obj : Pointer; const Block : Integer; const Value : Integer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Value - Значение, на которое будет увеличен счетчик блока
        [Obfuscation]
        public static int Increment(IntPtr obj, int block, int value)
        {
            throw new NotImplementedException();
            string text =
                $"!!! Increment !!!\tobj:{obj}\tBlock:{block}\tValue:{value}\t";
            WriteToLog(text);
            try
            {
                if (!USB.Ready())
                {
                    WriteToLog($"!!! WriteBlock ERROR _reader");
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
                }

                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(block, out controlSector, out blockInSector);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! Increment before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                    {
                        byte[] dataBadSectors = ReadFromFileSector(controlSector, blockInSector);
                        IncDecByteArray(ref dataBadSectors, value);
                        WriteOrReplaceToFileSector(controlSector, blockInSector, dataBadSectors);
                        //TODO убрать логи чтения и записи
                        //WriteToLog($"Incremented sector in file '{controlSector}':[{blockInSector}]{dataBadSectors.ByteArrayToString()}");
                        //WriteDataToLog(controlSector, blockInSector, dataBadSectors.ByteArrayToString(), true);

                        return (int)ErrorCodes.E_SUCCESS;
                    }

                    //byte[] data = GetData(controlSector, blockInSector, keyA, keyB).Result;
                    //IncDecByteArray(ref data, value);
                    //var result = SetData(data, controlSector, blockInSector, keyA, keyB).Result;

                    //if (result == (int)ErrorCodes.E_SUCCESS)
                    //    result = Transfer(obj, block);

                    ////TODO убрать логи чтения и записи
                    ////WriteToLog($"Incremented sector '{controlSector}':[{blockInSector}]{data.ByteArrayToString()}");
                    ////WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), true);

                    //return result;
                }
                //WriteToLog($"Increment ERROR !!! reader:{_reader} card: {_card}\r\n");
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Increment ERROR !!! {text}\r\n {e}");
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Восстановить значение блока (const Obj : Pointer; const Block : Integer) : Integer; stdcall;
        [Obfuscation]
        public static int Restore(IntPtr obj, int block)
        {
            return (int)ErrorCodes.E_SUCCESS;
            //throw new NotImplementedException();
            //string text =
            //    $"!!! Restore !!!\tobj:{obj}\tBlock:{block}\t";
            //WriteToLog(text);
            //try
            //{
            //    if (_card != null)
            //    {
            //        int blockInSector;
            //        int controlSector;
            //        BlockToSectorBlock(block, out controlSector, out blockInSector);

            //        var keyA = GetKeyFromCollection(controlSector, 0);
            //        var keyB = GetKeyFromCollection(controlSector, 1);

            //        text =
            //            $"!!! Restore before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
            //        WriteToLog(text);

            //        if (keyA != null)
            //            _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyA, Sector = controlSector, Key = keyA });
            //        if (keyB != null)
            //            _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyB, Sector = controlSector, Key = keyB });

            //        var sec = _card.GetSector(controlSector);

            //        //TODO только для тестов!
            //        //if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector))??false)
            //        //    return (int)ErrorCodes.E_SUCCESS;

            //        int result = (int)ErrorCodes.E_SUCCESS;
            //        if (sec.RestoreData(blockInSector).Result)
            //            result = Transfer(obj, block);

            //        return result;
            //    }
            //    WriteToLog($"Restore ERROR !!! reader:{_reader} card: {_card}\r\n");
            //    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            //}
            //catch (Exception e)
            //{
            //    WriteToLog($"Restore ERROR !!! {text}\r\n {e}");
            //    return (int)ErrorCodes.E_GENERIC;
            //}
        }

        // Применить изменения (const Obj : Pointer; const Block : Integer) : Integer; stdcall;
        [Obfuscation]
        public static int Transfer(IntPtr obj, int block)
        {
            return (int)ErrorCodes.E_SUCCESS;
            //throw new NotImplementedException();
            //string text =
            //    $"!!! Transfer !!!\tobj:{obj}\tBlock:{block}\t";
            ////WriteToLog(text);
            //try
            //{
            //    if (_card != null)
            //    {
            //        int blockInSector;
            //        int controlSector;
            //        BlockToSectorBlock(block, out controlSector, out blockInSector);

            //        var keyA = GetKeyFromCollection(controlSector, 0);
            //        var keyB = GetKeyFromCollection(controlSector, 1);

            //        text =
            //            $"!!! Transfer before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
            //        //WriteToLog(text);

            //        //TODO только для тестов!
            //        //if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
            //        //    return (int)ErrorCodes.E_SUCCESS;

            //        return FlushData(controlSector, blockInSector, keyA, keyB).Result;
            //    }
            //    WriteToLog($"Transfer ERROR !!! reader:{_reader} card: {_card}\r\n");
            //    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            //}
            //catch (Exception e)
            //{
            //    WriteToLog($"Transfer ERROR !!! {text}\r\n {e}");
            //    return (int)ErrorCodes.E_GENERIC;
            //}
        }
        //private static async Task<int> FlushData(int sector, int blockInSector, byte[] keyA, byte[] keyB)
        //{
        //    try
        //    {
        //        if (keyA != null)
        //            _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyA, Sector = sector, Key = keyA });
        //        if (keyB != null)
        //            _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyB, Sector = sector, Key = keyB });

        //        var sec = _card.GetSector(sector);
        //        await sec.Flush();
        //        if (blockInSector == sec.NumDataBlocks - 1)
        //        {
        //            if (keyA != null)
        //                _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyA, Sector = sector, Key = keyA });
        //            if (keyB != null)
        //                _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyB, Sector = sector, Key = keyB });

        //            if (keyA != null && keyB != null)
        //                await sec.FlushTrailer(keyA.ByteArrayToString(), keyB.ByteArrayToString());
        //        }
        //        return (int)ErrorCodes.E_SUCCESS;
        //    }
        //    catch (Exception e)
        //    {
        //        WriteToLog($"FlushData ERROR !!! \r\n {e}");
        //        return (int)ErrorCodes.E_GENERIC;
        //    }
        //}

        // Отключиться от карты (const Obj : Pointer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int Halt(IntPtr obj)
        {
            string text =
                $"!!! Halt !!!\tobj:{obj}\t";
            WriteToLog(text);
            try
            {
                //Reader_CardRemoved(null, null);
                SendCmd(Commands.Pass, new byte[] { 0x0 });
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"Halt ERROR !!! {text}\r\n {e}");
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        private static void WriteOrReplaceToFileSector(int sector, int block, byte[] data)
        {
            var uid = CardId;
            var hex = BitConverter.ToString(uid);
            hex = hex.Replace("-", "");
            if (uid == null)
            {
                WriteToLog("WriteOrReplaceToFileSector Попытка записи данных в неинициализированную карты");
                throw new Exception("Попытка записи данных в неинициализированную карты");
            }
            var LogPath = Path.Combine(LogirPath, $@"file_sector_{hex}.dat");

            var fileList = File.ReadAllLines(LogPath).ToList();
            var ind = fileList.FindIndex(s => s.Contains($"{sector},{block}:"));
            if (ind >= 0)
            {
                var dat = BitConverter.ToString(data);
                dat = hex.Replace("-", "");
                fileList[ind] = $"{sector},{block}:{dat}";
                File.WriteAllLines(LogPath, fileList);
            }
            else
                using (StreamWriter sw = File.AppendText(LogPath))
                {
                    var dat = BitConverter.ToString(data);
                    dat = hex.Replace("-", "");
                    sw.Write($"{sector},{block}:{dat}{Environment.NewLine}");
                }
        }
        private static byte[] ReadFromFileSector(int sector, int block)
        {
            var uid = CardId;
            var hex = BitConverter.ToString(uid);
            hex = hex.Replace("-", "");
            if (uid == null)
            {
                WriteToLog("ReadFromFileSector Попытка чтения данных с неинициализированной карты");
                throw new Exception();
            }
            var LogPath = Path.Combine(LogirPath, $@"file_sector_{hex}.dat");
            var sectors = new Dictionary<Tuple<int, int>, byte[]>();
            if (File.Exists(LogPath))
            {
                var fileLise = File.ReadAllLines(LogPath).ToList();
                fileLise.ForEach(s =>
                {
                    var arr = s.Split(':', ',');
                    if (arr.Length == 3)
                    {
                        int sec = Convert.ToInt32(arr[0]);
                        int bl = Convert.ToInt32(arr[1]);
                        var NumberChars = arr[2].Length;
                        var dat = new byte[NumberChars / 2];
                        for (var i = 0; i < NumberChars; i += 2)
                            dat[i / 2] = Convert.ToByte(arr[2].Substring(i, 2), 16);
                        sectors[new Tuple<int, int>(sec, bl)] = dat;
                    }
                });
            }

            byte[] data;
            return sectors.TryGetValue(new Tuple<int, int>(sector, block), out data) ? data : null;
        }
        private static HashSet<Tuple<int, int>> ReadBadsFromFileSector()
        {
            var uid = CardId;
            var hex = BitConverter.ToString(uid);
            hex = hex.Replace("-", "");
            if (uid == null)
            {
                WriteToLog("ReadBadsFromFileSector Попытка чтения данных с неинициализированной карты");
                throw new Exception("Попытка чтения данных с неинициализированной карты");
            }
            var LogPath = Path.Combine(LogirPath, $@"file_sector_{hex}.dat");

            var result = new HashSet<Tuple<int, int>>();

            if (File.Exists(LogPath))
            {
                using (var file = new StreamReader(LogPath))
                {
                    var header = file.ReadLine();
                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        var bads = header.Split(';');
                        foreach (var bad in bads)
                        {
                            var pair = bad.Split(',');
                            if (pair.Length == 2)
                                result.Add(
                                    new Tuple<int, int>(
                                        Convert.ToInt32(pair[0]),
                                        Convert.ToInt32(pair[1])));
                        }
                    }
                }
            }

            return result;
        }

        private static void BlockToSectorBlock(int block, out int sector, out int blockInSector)
        {
            //32 сектора по 4 блока
            int blocksSize4 = 32 * 4;
            int dataBlockSize = block < blocksSize4 ? 4 : 16;
            sector = (block < blocksSize4) ? (block / dataBlockSize) : ((block - blocksSize4) / dataBlockSize + 32);
            blockInSector = (block < blocksSize4 ? block : block - blocksSize4) % dataBlockSize;//TODO  ???????????
        }

        private static byte SectorToBlock(int sector)
        {
            //32 сектора по 4 блока
            int blocksSize4 = 32 * 4;
            if (sector > 32)
                return (byte) (blocksSize4 + ((sector - 32)*16));

            return (byte) (sector*4);
        }

        private static void IncDecByteArray(ref byte[] dst, int value)
        {
            var localArray = new byte[dst.Length + 1];
            Array.Copy(dst, 0, localArray, 1, dst.Length);
            localArray[0] = 0x00;
            BigInteger data = new BigInteger(localArray.Reverse().ToArray());
            data += value;
            localArray = data.ToByteArray();

            if (localArray.Length > dst.Length)
            {
                var diff = localArray.Length - dst.Length;
                Array.Copy(localArray.Reverse().ToArray(), diff, dst, 0, dst.Length);
            }
            else
            {
                var diff = dst.Length - localArray.Length;
                Array.Copy(localArray.Reverse().ToArray(), 0, dst, diff, localArray.Length);
            }
        }

        private static byte[] GetKeyFromCollection(int sector, int keyType)
        {
            Tuple<int, bool, byte[]> currentKey;
            if (_keys.TryGetValue(new Tuple<int, int>(sector, keyType), out currentKey))
            {
                return currentKey.Item3;
            }

            return null;
        }

        #endregion

        #region События USB

        private static void usb_OnSpecifiedDeviceArrived(object sender, EventArgs e)
        {

        }

        private static void usb_OnSpecifiedDeviceRemoved(object sender, EventArgs e)
        {
        }

        private static void usb_OnDeviceArrived(object sender, EventArgs e)
        {
            //this.listBox1.Items.Add("Found a Device");
        }

        private static void usb_OnDeviceRemoved(object sender, EventArgs e)
        {
            //if (InvokeRequired)
            //{
            //    Invoke(new EventHandler(usb_OnDeviceRemoved), new object[] { sender, e });
            //}
            //else
            //{
            //    this.listBox1.Items.Add("Device was removed");
            //}
        }

        private static void usb_OnDataSend(object sender, EventArgs e)
        {
            //isResponseReceived = false;
        }

        private static void usb_OnDataReceived(object sender, DataRecievedEventArgs args)
        {
            lock (LockObj)
            {
                string recData = "Raw data received: ";
                foreach (byte myData in args.data)
                {
                    if (myData.ToString("X2").Length == 1)
                    {
                        recData += "0";
                    }

                    recData += myData.ToString("X2") + " ";
                }

                WriteToLog(recData, false);

                var currentDataPos = _tempData?.Length ?? 0;

                if (args.data[0] == 0x1 || args.data[0] == 0x2)
                {
                    _tempData = new byte[args.data.Length];
                }
                if (args.data[0] == 0x2)
                {
                    _waitNextResponse = true;
                }
                if ((args.data[0] == 0x3 || args.data[0] == 0x4) && _waitNextResponse)
                {
                    Array.Resize(ref _tempData, currentDataPos + args.data.Length);
                }
                if ((args.data[0] == 0x4 && _waitNextResponse) || args.data[0] == 0x1)
                {
                    _waitNextResponse = false;
                }
                if (_tempData != null)
                    Array.Copy(args.data, currentDataPos == 0 ? 0 : 1, _tempData, currentDataPos, args.data.Length - (currentDataPos == 0 ? 0 : 1));

                if (!_waitNextResponse)
                {
                    //byte LastCommandCode = 0;
                    //Status LastCommandStatus = Status.OK;
                    //bool LastResponseCRCIsOK = false;
                    //byte[] responseData = { };
                     
                    if (ParseResponse(_tempData, ref LastCommandCode, ref LastCommandStatus, ref LastResponseCRCIsOK, ref LastResponseData))
                    {
                        Commands c = (Commands)(LastCommandCode * 256 + LastSubCommandCode);
                        WriteToLog($"parse response: {c}: {LastCommandStatus} CRC:{(LastResponseCRCIsOK ? "ОК" : "error") }", LastCommandStatus != Status.OK);
                        WriteToLog("commData:" + (LastResponseData.Length != 0 ? BitConverter.ToString(LastResponseData) : " empty"), LastCommandStatus != Status.OK);
                        if (Commands.Poll == c && LastResponseData.Length != 0)
                        {
                            WriteToLog($" Card: {(CardType)LastResponseData[0]}", LastCommandStatus != Status.OK);
                            CardType = (CardType)LastResponseData[0];
                            CardId = new byte[LastResponseData.Length - 1];
                            Array.Copy(LastResponseData, 1, CardId, 0, LastResponseData.Length - 1);
                        }
                        ResponseReceived.Set();
                    }
                    else
                        WriteToLog("wrong parsing response!", true);

                    _tempData = null;
                }
                else
                    WriteToLog("wait next response", false);
            }
        }

        #endregion

        #region Внутренние методы
        public static void Init(int timeout = 10000)
        {
            lock (LockObj)
            {
                WriteToLog("Init(int timeout = 10000)", false);
                if (timeout < 0) throw new ArgumentOutOfRangeException(nameof(timeout));
                InitTh.Start();
                for (int i = 0; i < timeout/10 && !USB.Ready(); i++)
                {
                    Thread.Sleep(10);
                }
            }
        }
        private static readonly  Thread InitTh = new Thread(InitInternal);
        private static void InitInternal()
        {
            if (USB.Ready())
            {
                return;
            }

            WriteToLog("InitInternal", false);

            USB.OnSpecifiedDeviceRemoved += usb_OnSpecifiedDeviceRemoved;
            USB.OnSpecifiedDeviceArrived += usb_OnSpecifiedDeviceArrived;
            USB.OnDeviceArrived += usb_OnDeviceArrived;
            USB.OnDeviceRemoved += usb_OnDeviceRemoved;
            USB.OnDataRecieved += usb_OnDataReceived;
            USB.OnDataSend += usb_OnDataSend;

            USB.ProductId = USBDevProduct;
            USB.VendorId = USBDevVendor;

            USB.Open(true);
        
            if (USB.Ready())
            {
                var vid = "";
                var pid = "";
                USB.GetInfoStrings(ref vid, ref pid);

                USBDevIRL = USB.SpecifiedDevice.InputReportLength;
                USBDevORL = USB.SpecifiedDevice.OutputReportLength;
                USBDevFRL = USB.SpecifiedDevice.FeatureReportLength;
            }
        }

        //private static bool Send(Commands cmd, byte[] data, int timeout = 2000)
        //{
        //    return SendCmd(cmd, data, timeout) == Status.OK;
        //    //var task = new Task<Status>(() => SendCmd(cmd, data, timeout));
        //    //task.Start();
        //    //if (task.Result == Status.OK)

        //    //return task.Result == Status.OK;
        //}
        public static Status SendCmd(Commands cmd, byte[] data, int timeout = 2000)
        {
            if (USB.SpecifiedDevice != null)
            {
                lock (LockObj)
                {
                    try
                    {
                        ClearResponseStatuses();
                        LastCommandCode = (byte) ((ushort)cmd >> 8);
                        LastSubCommandCode = (byte)cmd;

                        var command = BitConverter.GetBytes((ushort)cmd);
                        Array.Reverse(command);

                        LastCommandData = new byte[64];

                        int currCommandPos = 0;
                        Array.Copy(new[] { Convert.ToByte(1) }, 0, LastCommandData, currCommandPos, 1);
                        currCommandPos += 1;
                        Array.Copy(InitArray, 0, LastCommandData, currCommandPos, InitArray.Length);
                        currCommandPos += InitArray.Length;
                        Array.Copy(command, 0, LastCommandData, currCommandPos, command.Length);
                        currCommandPos += command.Length;
                        var dataLen = BitConverter.GetBytes((UInt16)data.Length);
                        Array.Reverse(dataLen);
                        Array.Copy(dataLen, 0, LastCommandData, currCommandPos, dataLen.Length);
                        currCommandPos += dataLen.Length;
                        if (data.Length > 0)
                        {
                            Array.Copy(data, 0, LastCommandData, currCommandPos, data.Length);
                            currCommandPos += data.Length;
                        }
                        var crc = BitConverter.GetBytes(CalculateCRC(LastCommandData, currCommandPos));
                        //if (cmd == Commands.Pass || inPassMode)
                        if (cmd == Commands.Pass || cmd == Commands.Poll ||
                            cmd == Commands.Auth || cmd == Commands.Read || cmd == Commands.Write)
                            Array.Reverse(crc);//Pass CRC in BIGEndian

                        Array.Copy(crc, 0, LastCommandData, currCommandPos, crc.Length);
                        //currCommandPos += crc.Length;

                        ResponseReceived.Reset();
                        USB.SpecifiedDevice.SendData(LastCommandData);
                        ResponseReceived.WaitOne(timeout);
                        return LastCommandStatus;
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex.ToString(), true);
                        return Status.Unknown_Exception;
                    }
                }
            }
            WriteToLog("Не найдено USB устройство", true);
            return Status.Command_Not_Sended;
        }

        public static bool ParseResponse(byte[] data, ref byte command, ref Status status, ref bool crcok, ref byte[] commData)
        {
            //var mode = data[0];
            var d1 = BitConverter.ToString(data, 1).Replace("-", "");
            var d2 = BitConverter.ToString(InitArray).Replace("-", "");
            if (d1.StartsWith(d2))
            {
                var bodyLength = data.Length - (1 + InitArray.Length);
                var body = new byte[bodyLength];
                Array.Copy(data, 1 + InitArray.Length, body, 0, bodyLength);
                command = body[0];
                status = (Status)body[1];
                var lengthRaw = new byte[2];
                Array.Copy(body, 2, lengthRaw, 0, 2);
                Array.Reverse(lengthRaw);
                var length = BitConverter.ToInt16(lengthRaw, 0);
                commData = new byte[length];
                Array.Copy(body, 4, commData, 0, length);
                var crcResponse = BitConverter.ToUInt16(body, length + 4);
                var rawCRCCalc = BitConverter.GetBytes(CalculateCRC(data, length + 15));
                Array.Reverse(rawCRCCalc);//Responce  CRC in BIGEndian
                var crcCalc = BitConverter.ToUInt16(rawCRCCalc, 0);
                crcok = crcResponse == crcCalc;

                return true;
            }
            return false;
        }

        private static UInt16[] CrcTable = {
          0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7, 0x8108, 0x9129,
          0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF, 0x1231, 0x0210, 0x3273, 0x2252,
          0x52B5, 0x4294, 0x72F7, 0x62D6, 0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C,
          0xF3FF, 0xE3DE, 0x2462, 0x3443, 0x0420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485,
          0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D, 0x3653, 0x2672,
          0x1611, 0x0630, 0x76D7, 0x66F6, 0x5695, 0x46B4, 0xB75B, 0xA77A, 0x9719, 0x8738,
          0xF7DF, 0xE7FE, 0xD79D, 0xC7BC, 0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861,
          0x2802, 0x3823, 0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
          0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12, 0xDBFD, 0xCBDC,
          0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A, 0x6CA6, 0x7C87, 0x4CE4, 0x5CC5,
          0x2C22, 0x3C03, 0x0C60, 0x1C41, 0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B,
          0x8D68, 0x9D49, 0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0x0E70,
          0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78, 0x9188, 0x81A9,
          0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F, 0x1080, 0x00A1, 0x30C2, 0x20E3,
          0x5004, 0x4025, 0x7046, 0x6067, 0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C,
          0xE37F, 0xF35E, 0x02B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
          0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D, 0x34E2, 0x24C3,
          0x14A0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405, 0xA7DB, 0xB7FA, 0x8799, 0x97B8,
          0xE75F, 0xF77E, 0xC71D, 0xD73C, 0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657, 0x7676,
          0x4615, 0x5634, 0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB,
          0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882, 0x28A3, 0xCB7D, 0xDB5C,
          0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A, 0x4A75, 0x5A54, 0x6A37, 0x7A16,
          0x0AF1, 0x1AD0, 0x2AB3, 0x3A92, 0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B,
          0x9DE8, 0x8DC9, 0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1,
          0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8, 0x6E17, 0x7E36,
          0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0x0ED1, 0x1EF0
        };

        private static void ClearResponseStatuses()
        {
            LastCommandCode = 0;
            LastCommandStatus = Status.Empty;
            LastResponseCRCIsOK = false;
            _waitNextResponse = false;
            CardId = new byte[] {};
        }

        public static UInt16 CalculateCRC(byte[] buffer, int len)
        {
            ushort crc = 0xffff;
            int buffPos = 1;
            len -= 1;
            while ((len--) > 0)
            {
                ushort crcShiftedRight = (ushort)(crc >> 8);
                ushort crcShiftedLeft = (ushort)(crc << 8);

                crc = (ushort)(CrcTable[crcShiftedRight ^ buffer[buffPos]] ^ crcShiftedLeft);
                buffPos++;
            }
            return (crc);
        }

        private static int _writeCount;
        private static void WriteToLog(string text, bool isError = false)
        {
            bool writeToLog = isError;
#if DEBUG
            writeToLog = true;
#endif
            //TODO на время тестирования
            writeToLog = true;

            if (!writeToLog || string.IsNullOrWhiteSpace(LogirPath))
                return;

            var logPath = Path.Combine(LogirPath, @"DLL_Log_VIVO_Pay.txt");
            if (/*write_count == 0 ||*/ !File.Exists(logPath))
            {
                using (StreamWriter sw = File.CreateText(logPath))
                {
                    sw.Write($"[{_writeCount}\t{DateTime.Now}]: {text}\r\n");
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    sw.Write($"[{_writeCount}\t{DateTime.Now}]: {text}\r\n");
                }
            }
            ++_writeCount;
            if (_writeCount == int.MaxValue)
                _writeCount = 0;
        }

        #endregion
    }
}