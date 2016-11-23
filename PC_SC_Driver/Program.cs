using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoverConstants;
using CustomMifareReader;
using MarshalHelper;
using MiFare;
using MiFare.Classic;
using MiFare.Devices;
using MiFare.PcSc;

namespace CustomMifareReaderm
{
    public static class PcScDrv
    {
        private static SmartCardReader _reader = null;
        private static MiFareCard _card = null;
        private static MiFareCard _localCard = null;
        private static IccDetection _cardIdentification = null;

        private static int? _currentSector = null;
        private static int? _currentKeyType = null;

        private static IntPtr _obj = IntPtr.Zero;
        private static readonly string[,] TestCard =
        {
            {""},
            {"00000000000000000000000000000000"}, // 1
            {"00000000000000000000000000000000"}, // 2
            {""},
            {"30303030303100000000000000000000"}, // 4
            {"00000000000100000002000000DD0002"}, // 5
            {"4A8CEE9713D5E4400000000000000000"}, // 6
            {""},
            {"00000000000000000000000000000000"}, // 8
            {"E8030000C409000088130000FF014300"}, // 9
            {"00000000000000000000000000000000"}, // 10
            {""},
            {"00000000000000000000000000000000"}, // 12
            {"00000000000000000000000000000000"}, // 13
            {"00000000000000000000000000000000"}, // 14
            {""},
            {"00000000000000000000000000000000"}, // 16
            {"E8030000C409000088130000FF024000"}, // 17
            {"00000000000000000000000000000000"}, // 18
            {""},
            {"00000000000000000000000000000000"}, // 20
            {"00000000000000000000000000000000"}, // 21
            {"00000000000000000000000000000000"}, // 22
            {""},
            {"00000000000000000000000000000000"}, // 24
            {"E8030000C409000088130000FF034100"}, // 25
            {"00000000000000000000000000000000"}, // 26
            {""},
            {"00000000000000000000000000000000"}, // 28
            {"00000000000000000000000000000000"}, // 29
            {"00000000000000000000000000000000"}, // 30
            {""},
            {"00000000000000000000000000000000"}, // 32
            {"E8030000C409000088130000FF044600"}, // 33
            {"00000000000000000000000000000000"}, // 34
            {""},
            {"00000000000000000000000000000000"}, // 36
            {"00000000000000000000000000000000"}, // 37
            {"00000000000000000000000000000000"}, // 38
            {""},
            {"00000000000000000000000000000000"}, // 40
            {"E8030000C409000088130000FF054700"}, // 41
            {"00000000000000000000000000000000"}, // 42
            {""},
            {"00000000000000000000000000000000"}, // 44
            {"00000000000000000000000000000000"}, // 45
            {"00000000000000000000000000000000"}, // 46
            {""},
            {"00000000000000000000000000000000"}, // 48
            {"E8030000C409000088130000FF064400"}, // 49
            {"00000000000000000000000000000000"}, // 50
            {""},
            {"00000000000000000000000000000000"}, // 52
            {"00000000000000000000000000000000"}, // 53
            {"00000000000000000000000000000000"}, // 54
            {""},
            {"9EFBFFFF620400006204000062040000"}, // 56
            {"A086010090D0030020A10700FF07E400"}, // 57
            {"E078C7C013D5E4400000000000000000"}, // 58
            {""},
            {"00000000000000000000000000000000"}, // 60
            {"00000000000000000000000000000000"}, // 61
            {"00000000000000000000000000000000"} // 62
        };

        //--------------------------sector,KeyType ---> slot, nonvolatile, key (6 bite)
        private static Dictionary< Tuple<int,int>, Tuple<int, bool, byte[]> > _keys;

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
        public static int GetErrorDescription(int ErrorCode, [MarshalAs(UnmanagedType.LPWStr)] ref string DescriptionBuf, int BufLen, IntPtr Obj)
        {
            string text =
                $"!!! GetErrorDescription !!!\tobj:{Obj}\terr:{ErrorCode}\tDescriptionBuf:{DescriptionBuf}\tbufLen:{BufLen}\t";
            WriteToLog(text);
            try
            {
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"GetErrorDescription ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Получить список логических устройств (const ItemsBuf : PWideChar; const BufLen : Integer; const Obj : Pointer) : Integer; stdcall;
        //   ItemsBuf - выходной буфер. Названия логических устройств разделены символом #0
        //   BufLen - длина выходного буфера в символах
        [Obfuscation]
        public static int GetLogicalDevices([MarshalAs(UnmanagedType.LPWStr)] ref IntPtr ItemsBuf, ref  int BufLen, IntPtr Obj)
        {
            string text =
                $"!!! GetLogicalDevices !!!\tobj:{Obj}\tItemsBuf:{ItemsBuf}\tbufLen:{BufLen}\t";
            WriteToLog(text);
            try
            {
                var readers = CardReader.GetReaderNames().ToArray();
                string res = "";
                Array.ForEach(readers, t => res += (String.IsNullOrWhiteSpace(res) ? "" : "#0") + t);
                res += '\0';
                BufLen = readers.Length;
                UnMemory<char>.SaveInMemArr(res.ToCharArray(), ref ItemsBuf);

                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"GetLogicalDevices ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Инициализация считывателя (const InitStr : PWideChar; const Caps : PMifareClassicReaderCaps; const Obj : PPointer) : Integer; stdcall;
        //   InitParams - параметры инициализации. Param1=Value1;Param2=Value2...
        //   Caps - возможности считывателя
        //   Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int Init([MarshalAs(UnmanagedType.LPWStr)] string InitStr, IntPtr Caps, ref IntPtr Obj)
        {
            string text =
                $"!!! Init !!!\tobj:{Obj}\tCaps:{Caps}\tInitStr:{InitStr ?? "null"}\t";
            WriteToLog(text);
            try
            {
                UnMemory<int>.SaveInMemArr(new int[] {12,1,0}, ref Caps);

                var caps = UnMemory<int>.ReadInMemArr(Caps, 3);

                text =
                    $"!!! Init Caps !!!\tsize:{caps[0]}\tVolatileKeySlotCount:{caps[1]}\tNonvolatileKeySlotCount:{caps[2]}\t";
                WriteToLog(text);

                var res = InitializeReader().Result;
                // выделение  памяти под obj
                int _pInt = 0;
                UnMemory<int>.SaveInMem(_pInt, ref Obj);
                _obj = Obj;

                if (_keys == null)
                    _keys = new Dictionary<Tuple<int, int>, Tuple<int, bool, byte[]>>();
                else
                    _keys.Clear();

                return res;
            }
            catch (Exception e)
            {
                WriteToLog($"Init ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
        }

        // Деинициализация считывателя (IntPtr Obj);
        //   Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int Deinit(IntPtr Obj)
        {
            string text =
                $"!!! Deinit !!!\tobj:{Obj}\t";
            WriteToLog(text);
            try
            {
                _keys.Clear();
                // очищаем память для выделенных объектов
                //UnMemory.FreeMemory();
                Marshal.FreeCoTaskMem(_obj);
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"Deinit ERROR!!! {text}\r\n {e.ToString()}", true);
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
        public static int LoadKey(IntPtr Obj, int Sector, int KeyType_int, bool NonvolatileMemory, int KeyIndex, IntPtr Key)
        {
            string text =
                $"!!! LoadKey !!!\tobj:{Obj}\tSector:{Sector}\tKeyType:{KeyType_int}\tNonvolatileMemory:{NonvolatileMemory}\tKeyIndex:{KeyIndex}\tKey:{Key}\t";
            WriteToLog(text);
            try
            {
                if (_keys != null && _localCard != null)
                {
                    _currentSector = Sector;
                    _currentKeyType = KeyType_int;
                    byte[] key = UnMemory<byte>.ReadInMemArr(Key, 6);
                    _keys[new Tuple<int, int>(Sector, KeyType_int)] = new Tuple<int, bool, byte[]>(KeyIndex, NonvolatileMemory, key);
                    _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = (KeyType_int == 0 ? KeyType.KeyA : KeyType.KeyB), Sector = Sector, Key = key });

                    text =
                        $"!!! LoadKey over !!!\tSector:{Sector}\tKeyType:{KeyType_int}\tKeyIndex:{KeyIndex}\tkey:{BitConverter.ToString(key ?? new byte[] { })}\t";
                    WriteToLog(text);
                }   
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"LoadKey ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_POS_KEYS_LOAD;
            }
        }

        // Поиск стандартных меток
        // Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int RequestStandard(IntPtr Obj)
        {
            string text =
                $"!!! RequestStandard !!!\tobj:{Obj}\t";
            WriteToLog(text);
            try
            {
                InitCard();

                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"RequestStandard ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Поиск всех меток
        // Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int RequestAll(IntPtr Obj)
        {
            string text =
                $"!!! RequestAll !!!\tobj:{Obj}\t";
            WriteToLog(text);
            try
            {
                InitCard();

                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"RequestAll ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Получение номера какой-либо метки (const Obj : Pointer; const SerialNumberBuf : Pointer; const BufSize : Integer; const SerialNumberSize : PInteger) : Integer;
        //   Obj - ссылка на объект считывателя
        //   SerialNumberBuf - выходной буфер для аппаратного номера карты
        //   BufSize - размер выходного буфера
        //   SerialNumberSize - размер считанного аппаратного номера карты в байтах (обычно 4 или 7)
        [Obfuscation]
        public static int Anticollision(IntPtr Obj, IntPtr SerialNumberBuf, int BufSize, IntPtr SerialNumberSize)
        {
            string text =
                $"!!! Anticollision !!!\tobj:{Obj}\tSerialNumberBuf:{SerialNumberBuf}\tBufSize:{BufSize}\tSerialNumberSize:{SerialNumberSize}\t";
            WriteToLog(text);
            try
            {
                var uid = _localCard?.GetUid().Result;
                if (uid != null)
                {
                    byte[] truncUid = new byte[Math.Min(BufSize, uid.Length)];
                    Array.Copy(uid, truncUid, truncUid.Length);
                    UnMemory<int>.SaveInMem(truncUid.Length, ref SerialNumberSize);
                    UnMemory<byte>.SaveInMemArr(uid, ref SerialNumberBuf);
                    //Marshal.StructureToPtr(memory_object, SerialNumberBuf, true);

                    var uidWrited = UnMemory<byte>.ReadInMemArr(SerialNumberBuf, truncUid.Length);
                    var serialNumberSizeWrited = UnMemory<int>.ReadInMem(SerialNumberSize);

                    text =
                        $"!!! Anticollision over !!!\tobj:{Obj}\tSerialNumberBuf:{SerialNumberBuf}\tBufSize:{BufSize}\tuid:{BitConverter.ToString(uid)}\tuid_w:{BitConverter.ToString(uidWrited ?? new byte[] { })}\tSerialNumberSize:{SerialNumberSize}\tSerialNumberSize_writed:{serialNumberSizeWrited}\t";
                    WriteToLog(text);
                    return (int)ErrorCodes.E_SUCCESS;
                }
                return (int)ErrorCodes.E_SUCCESS;
                //return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Anticollision ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Выбор метки по номеру
        //   Obj - ссылка на объект считывателя
        //   SerialNumber - серийный номер метки
        //   SerialNumberSize - размер серийного номера метки в байтах
        [Obfuscation]
        public static int SelectCard(IntPtr Obj, IntPtr SerialNumber, int SerialNumberSize)
        {
            string text =
                $"!!! SelectCard !!!\tobj:{Obj}\tSerialNumber:{SerialNumber}\tSerialNumberSize:{SerialNumberSize}\t";
            WriteToLog(text);
            try
            {
                byte[] uid = UnMemory<byte>.ReadInMemArr(SerialNumber, SerialNumberSize);

                text =
                    $"!!! SelectCard over !!!\tobj:{Obj}\tSerialNumber:{SerialNumber}\tSerialNumberSize:{SerialNumberSize}\tuid:{BitConverter.ToString(uid ?? new byte[] { })}\t";
                WriteToLog(text);
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"SelectCard ERROR!!! {text}\r\n {e.ToString()}", true);
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
        public static int Authentication(IntPtr Obj, int Sector, int KeyType, bool NonvolatileMemory, int KeyIndex)
        {
            string text =
                $"!!! Authentication !!!\tobj:{Obj}\tSector:{Sector}\tKeyType:{KeyType}\tNonvolatileMemory:{NonvolatileMemory}\tKeyIndex:{KeyIndex}\t";
            WriteToLog(text);
            try
            {
                if (_reader == null || _card == null)
                    throw new Exception("Аутентификация невозможна, ридер или карта не инициализированы");

                //TODO только для тестов!
                if (Sector == 2)
                    return (int)ErrorCodes.E_SUCCESS;

                var secAccess = _card.GetSector(Sector).Access;
                if (secAccess == null)
                    throw new Exception("Неудачная аутентификация!");

                _currentSector = Sector;
                _currentKeyType = KeyType;
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"Authentication ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Считать блок (const Obj : Pointer; const Block : Integer; const Buffer : Pointer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Buffer - буфер карты, 16 байт
        [Obfuscation]
        public static int ReadBlock(IntPtr Obj, int Block, IntPtr Buffer)
        {
            string text =
                $"!!! ReadBlock !!!\tobj:{Obj}\tBlock:{Block}\tBuffer:{Buffer}\t";
            WriteToLog(text);
            try
            {
                if (_localCard != null && _currentSector != null && _currentKeyType != null)
                {
                    int blocksSize4 = 32*4;
                    int NumDataBlocks = Block < blocksSize4 ? 4 : 16;
                    int blockInSector = (Block < blocksSize4 ? Block : Block - blocksSize4) % NumDataBlocks;//TODO  ???????????
                    var key = _keys[new Tuple<int, int>(_currentSector.Value, _currentKeyType.Value)].Item3;
                    text =
                        $"!!! ReadBlock before !!!\tcurrentSector: {_currentSector.Value}\tblockInSector: {blockInSector}\t_currentKeyType: {_currentKeyType.Value}\tkey: {BitConverter.ToString(key ?? new byte[] {})}";
                    WriteToLog(text);

                    byte[] data;
                    if (true)
                    {
                        _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = (_currentKeyType.Value == 0 ? KeyType.KeyA : KeyType.KeyB), Sector = _currentSector.Value, Key = key });
                        //_localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = _currentSector.Value, Key = key });
                        //_localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = _currentSector.Value, Key = key });

                        var sec = _localCard.GetSector(_currentSector.Value);
                        data = sec.GetData(blockInSector).Result;
                        //data = _localCard.GetData(_currentSector.Value, blockInSector, 16).Result;
                    }
                    else
                    {
                        var key1 = new byte[] { 0x27, 0xA2, 0x9C, 0x10, 0xF8, 0xC7 };
                        var key2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

                        if (_currentSector.Value == 1)
                        {
                            _localCard.AddOrUpdateSectorKeySet(new SectorKeySet(){KeyType = KeyType.KeyA,Sector = _currentSector.Value,Key = key1});
                            var sec = _localCard.GetSector(_currentSector.Value);
                            data = sec.GetData(blockInSector).Result;
                        }
                        else
                        {
                            _localCard.AddOrUpdateSectorKeySet(new SectorKeySet(){KeyType = KeyType.KeyA,Sector = 1,Key = key2});
                            //localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = 1, Key = key2 });
                            data = _localCard.GetData(_currentSector.Value, blockInSector, 16).Result;
                        }
                    }

                    if (data.Length > 0 && data.Length < 17)
                    {
                        WriteToLog($"SaveInMemory {data.Length} byte");
                        //card.Reader
                        //if (_card.Reader.GetType() == typeof(MiFareWin32CardReader)
                        //    && (_card.Reader as MiFareWin32CardReader).SmartCard.ReaderName.Contains("FEIG"))
                        //    data = data.Reverse().ToArray();

                        UnMemory<byte>.SaveInMemArr(data, ref Buffer);
                    }

                    string hexString = "";
                    for (int i = 0; i < data.Length; i++)
                    {
                        hexString += data[i].ToString("X2") + " ";
                    }
                    //TODO убрать логи чтения и записи
                    WriteToLog($"Sector '{_currentSector.Value}':[{blockInSector}]{hexString}");
                    WriteDataToLog(_currentSector.Value, blockInSector, hexString);

                    return (int)ErrorCodes.E_SUCCESS;
                }
                //return (int)ErrorCodes.E_CARDREADER_NOT_INIT;//TODO
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ReadBlock ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_SUCCESS;
                //return (int)ErrorCodes.E_GENERIC;//TODO
            }
        }

        // Записать блок (const Obj : Pointer; const Block : Integer; const Data : Pointer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Data - данные блока для записи (16 байт)
        [Obfuscation]
        public static int WriteBlock(IntPtr Obj, int Block, IntPtr Data)
        {
            string text =
                $"!!! WriteBlock !!!\tobj:{Obj}\tBlock:{Block}\tData:{Data}\t";
            WriteToLog(text);
            try
            {

                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Уменьшить значение Value-блока (const Obj : Pointer; const Block : Integer; const Value : Integer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Value - Значение, на которое будет уменьшен счетчик блока
        [Obfuscation]
        public static int Decrement(IntPtr Obj, int Block, int Value)
        {
            string text =
                $"!!! Decrement !!!\tobj:{Obj}\tBlock:{Block}\tValue:{Value}\t";
            WriteToLog(text);
            try
            {
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Увеличить значение Value-блока (const Obj : Pointer; const Block : Integer; const Value : Integer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        //   Block - индекс блока [0..255] (считается от начала карты, не от начала сектора)
        //   Value - Значение, на которое будет увеличен счетчик блока
        [Obfuscation]
        public static int Increment(IntPtr Obj, int Block, int Value)
        {
            string text =
                $"!!! Increment !!!\tobj:{Obj}\tBlock:{Block}\tValue:{Value}\t";
            WriteToLog(text);
            try
            {
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Восстановить значение блока (const Obj : Pointer; const Block : Integer) : Integer; stdcall;
        [Obfuscation]
        public static int Restore(IntPtr Obj, int Block)
        {
            string text =
                $"!!! Restore !!!\tobj:{Obj}\tBlock:{Block}\t";
            WriteToLog(text);
            try
            {
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Применить изменения (const Obj : Pointer; const Block : Integer) : Integer; stdcall;
        [Obfuscation]
        public static int Transfer(IntPtr Obj, int Block)
        {
            string text =
                $"!!! Transfer !!!\tobj:{Obj}\tBlock:{Block}\t";
            WriteToLog(text);
            try
            {
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        // Отключиться от карты (const Obj : Pointer) : Integer; stdcall;
        //   Obj - ссылка на объект считывателя
        [Obfuscation]
        public static int Halt(IntPtr Obj)
        {
            string text =
                $"!!! Halt !!!\tobj:{Obj}\t";
            WriteToLog(text);
            try
            {
                //Reader_CardRemoved(null, null);
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        private static int write_count = 0;
        private static void WriteToLog(string text, bool showMsg = false)
        {
            //bool logIsError = text.Contains("ERROR!!!");
            //bool writeToLog = logIsError;
            //#if DEBUG
            //        writeToLog = true;
            //#endif

            //if (!writeToLog)
            //    return;

            string path = @"C:\MifareServio\Out\DLL_Log.txt";
            // This text is added only once to the file.
            if (write_count == 0 || !File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.Write($"[{write_count}\t{DateTime.Now.ToString()}]: {text}\r\n");
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.Write($"[{write_count}\t{DateTime.Now.ToString()}]: {text}\r\n");
                }
            }
//        if (showMsg)
//            MessageBox.Show(text);
            ++write_count;
        }

        private static int write_data_count = 0;
        private static void WriteDataToLog(int sector, int block, string data, bool showMsg = false)
        {
            //bool logIsError = text.Contains("ERROR!!!");
            //bool writeToLog = logIsError;
            //#if DEBUG
            //        writeToLog = true;
            //#endif

            //if (!writeToLog)
            //    return;

            string path = @"C:\MifareServio\Out\DLL_Data_Log.txt";
            // This text is added only once to the file.
            if (write_data_count == 0 || !File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.Write($"sector: {sector} [{block}]\t{DateTime.Now.ToString()}]: {data}\r\n");
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.Write($"sector: {sector} [{block}]\t{DateTime.Now.ToString()}]: {data}\r\n");
                }
            }
            //        if (showMsg)
            //MessageBox.Show(data);
            ++write_data_count;
        }

        private static async Task<int> InitializeReader()
        {
            WriteToLog("InitializeReader");
            try
            {
                var readers = CardReader.GetReaderNames().ToArray();
                string currReader = "";
                if (readers.Length > 0)
                {
                    var r = new ReaderSelect();
                    r.Init(readers);
                    r.ShowDialog();
                    currReader = r?.CurrentReader;
                }

                WriteToLog($"select reader:{currReader}");

                if (_reader != null)
                {
                    _reader.CardAdded -= Reader_CardAdded;
                    _reader.CardRemoved -= Reader_CardRemoved;
                    _reader = null;
                }
                if (CardReader.GetReaderNames().Contains(currReader))
                {
                    _reader = await CardReader.FindAsync(currReader);
                    if (_reader != null)
                    {
                        _reader.CardAdded += Reader_CardAdded;
                        _reader.CardRemoved += Reader_CardRemoved;
                    }
                }
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! InitializeReader\r\n {e.ToString()}", true);
                throw e;
            }
        }

        private static void Reader_CardRemoved(object sender, CardRemovedEventArgs ev)
        {
            WriteToLog("Reader_CardRemoved");
            try
            {
                _localCard?.Dispose();
                _localCard = null;
                _card?.Dispose();
                _card = null;
                _currentSector = null;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! Reader_CardRemoved\r\n {e.ToString()}", true);
                throw e;
            }
        }

        private static void Reader_CardAdded(object sender, CardAddedEventArgs ev)
        {
            WriteToLog("Reader_CardAdded");
            try
            {
                _card?.Dispose();
                _card = ev.SmartCard.CreateMiFareCard();
                _localCard = _card;
                _currentSector = null;
            }
            catch (Exception e)
            {
                WriteToLog($"ERROR!!! Reader_CardAdded\r\n {e.ToString()}", true);
                throw e;
            }
        }

        private static async void InitCard()
        {
            WriteToLog("InitCard");
            try
            {
                //        WriteToLog("InitCard", true);
                if (_localCard == null)
                    throw new Exception("Карта не найдена!");
                _cardIdentification = await _localCard.GetCardInfo();
                WriteToLog("Connected to card\r\nPC/SC device class: " + _cardIdentification.PcscDeviceClass.ToString() +
                           "\r\nCard name: " + _cardIdentification.PcscCardName.ToString());
            }
            catch (Exception e)
            {
                WriteToLog("ERROR!!! InitCard\r\n" + e.Message);
                throw e;
            }
        }

        private static async void readCard()
        {
            WriteToLog("readCard");
            try
            { 
                if (_localCard != null && _cardIdentification != null && _cardIdentification.PcscDeviceClass == DeviceClass.StorageClass
                    && (_cardIdentification.PcscCardName == CardName.MifareStandard1K || _cardIdentification.PcscCardName == CardName.MifareStandard4K))
                {
                    // Handle MIFARE Standard/Classic
                    WriteToLog("MIFARE Standard/Classic card detected");

                    var uid = await _localCard.GetUid();
                    WriteToLog("UID:  " + BitConverter.ToString(uid));

                    // 16 sectors, print out each one
                    var key1 = new byte[] { 0x27, 0xA2, 0x9C, 0x10, 0xF8, 0xC7 };
                    var key2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                    //            Array.Reverse(key);
                    //            _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = 1, Key = key1 });

                    for (var sector = 0; sector< 16 && _card != null; sector++)
                    {
                        for (var block = 0; block< 4; block++)
                        {
                            try
                            {
                                //_localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = key2 });
                                //var sec = _localCard.GetSector(1);
                                //var d = await sec.GetData(0);
                                var data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                                if (sector == 1)
                                {
                                    _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = key1 });
                                    var sec = _localCard.GetSector(sector);
                                    data = await sec.GetData(block);
                                }
                                else
                                {
                                    _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = 1, Key = key2 });
                                    //_localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = 1, Key = key2 });
                                    data = await _localCard.GetData(sector, block, 16);
                                }

                                string hexString = "";
                                for (int i = 0; i<data.Length; i++)
                                {
                                    hexString += data[i].ToString("X2") + " ";
                                }

                                WriteToLog($"Sector '{sector}':{hexString}");

                            }
                            catch (Exception ex)
                            {
                                WriteToLog("Failed to load sector: " + sector + "\r\nEx: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteToLog("ERROR!!! readCard\r\n" + e.ToString());
                throw e;
            }
        }

        private static async void writeTestCard()
        {
            WriteToLog("readCard");
            try
            {
                if (_localCard != null && _cardIdentification != null && _cardIdentification.PcscDeviceClass == DeviceClass.StorageClass
                    && (_cardIdentification.PcscCardName == CardName.MifareStandard1K || _cardIdentification.PcscCardName == CardName.MifareStandard4K))
                {
                    // Handle MIFARE Standard/Classic
                    WriteToLog("MIFARE Standard/Classic card detected");

                    var uid = await _localCard.GetUid();
                    WriteToLog("UID:  " + BitConverter.ToString(uid));

                    // 16 sectors, print out each one
                    var key1 = new byte[] { 0x27, 0xA2, 0x9C, 0x10, 0xF8, 0xC7 };
                    var key2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                    //            Array.Reverse(key);
                    //            _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = 1, Key = key1 });

                    for (var sector = 0; sector < 16 && _card != null; sector++)
                    {
                        for (var block = 0; block < 4; block++)
                        {
                            try
                            {
                                //_localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = key2 });
                                //var sec = _localCard.GetSector(1);
                                //var d = await sec.GetData(0);
                                var data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                                if (sector == 1)
                                {
                                    _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = key1 });
                                    var sec = _localCard.GetSector(sector);
                                    data = await sec.GetData(block);
                                }
                                else
                                {
                                    _localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = 1, Key = key2 });
                                    //_localCard.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = 1, Key = key2 });
                                    data = await _localCard.GetData(sector, block, 16);
                                }

                                string hexString = "";
                                for (int i = 0; i < data.Length; i++)
                                {
                                    hexString += data[i].ToString("X2") + " ";
                                }

                                WriteToLog($"Sector '{sector}':{hexString}");

                            }
                            catch (Exception ex)
                            {
                                WriteToLog("Failed to load sector: " + sector + "\r\nEx: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteToLog("ERROR!!! readCard\r\n" + e.ToString());
                throw e;
            }
        }


    }
}