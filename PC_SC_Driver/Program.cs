using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoverConstants;
using CustomMifareReader;
using MarshalHelper;
using MiFare;
using MiFare.Classic;
using MiFare.Devices;
using MiFare.PcSc;
using System.Numerics;
using System.Windows.Forms;

namespace CustomMifareReaderm
{
    public static class PcScDrv
    {
        private static SmartCardReader _reader = null;
        private static MiFareCard _card = null;
        private static IccDetection _cardIdentification = null;
        private static HashSet<Tuple<int, int>> _cardBadSectors = null;

        private static IntPtr _obj = IntPtr.Zero;

        private static bool _isInfoNoCardShow = false;
        //--------------------------sector,KeyType ---> slot, nonvolatile, key (6 bite)
        private static Dictionary<Tuple<int, int>, Tuple<int, bool, byte[]>> _keys;

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
        public static int GetErrorDescription(int ErrorCode, [MarshalAs(UnmanagedType.LPWStr)] ref string DescriptionBuf,
            int BufLen, IntPtr Obj)
        {
            string text =
                $"!!! GetErrorDescription !!!\tobj:{Obj}\terr:{ErrorCode}\tDescriptionBuf:{DescriptionBuf}\tbufLen:{BufLen}\t";
            WriteToLog(text);
            try
            {
                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"GetErrorDescription ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_GENERIC;
            }
        }

        // Получить список логических устройств (const ItemsBuf : PWideChar; const BufLen : Integer; const Obj : Pointer) : Integer; stdcall;
        //   ItemsBuf - выходной буфер. Названия логических устройств разделены символом #0
        //   BufLen - длина выходного буфера в символах
        [Obfuscation]
        public static int GetLogicalDevices([MarshalAs(UnmanagedType.LPWStr)] ref IntPtr ItemsBuf, ref int BufLen,
            IntPtr Obj)
        {
            string text =
                $"!!! GetLogicalDevices !!!\tobj:{Obj}\tItemsBuf:{ItemsBuf}\tbufLen:{BufLen}\t";
            WriteToLog(text);
            try
            {
                var readers = CardReader.GetReaderNames().ToArray();
                string res = "";
                Array.ForEach(readers, t => res += (string.IsNullOrWhiteSpace(res) ? "" : "#0") + t);
                res += '\0';
                BufLen = readers.Length;
                text = $"!!! GetLogicalDevices !!!\tLogicalDevices:{res}";
                WriteToLog(text);

                UnMemory<char>.SaveInMemArr(res.ToCharArray(), ref ItemsBuf);

                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"GetLogicalDevices ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_GENERIC;
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
                UnMemory<int>.SaveInMemArr(new int[] {12, 1, 0}, ref Caps);

                var caps = UnMemory<int>.ReadInMemArr(Caps, 3);

                text =
                    $"!!! Init internal Caps !!!\tsize:{caps[0]}\tVolatileKeySlotCount:{caps[1]}\tNonvolatileKeySlotCount:{caps[2]}\t";
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
                WriteToLog($"Init ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_CARDREADER_NOT_INIT;
            }
        }

        // Деинициализация считывателя (IntPtr Obj);
        // Obj - ссылка на объект считывателя
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
                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"Deinit ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_GENERIC;
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
                if (_keys != null && _card != null)
                {
                    byte[] key = UnMemory<byte>.ReadInMemArr(Key, 6);
                    _keys[new Tuple<int, int>(Sector, KeyType_int)] = new Tuple<int, bool, byte[]>(KeyIndex, NonvolatileMemory, key);

                    text =
                        $"!!! LoadKey over !!!\tSector:{Sector}\tKeyType:{KeyType_int}\tKeyIndex:{KeyIndex}\tkey:{BitConverter.ToString(key ?? new byte[] {})}\t";
                    WriteToLog(text);
                }
                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"LoadKey ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_POS_KEYS_LOAD;
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

                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"RequestStandard ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_GENERIC;
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

                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"RequestAll ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
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
                var uid = _card?.GetUid().Result;
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
                        $"!!! Anticollision over !!!\tobj:{Obj}\tSerialNumberBuf:{SerialNumberBuf}\tBufSize:{BufSize}\tuid:{BitConverter.ToString(uid)}\tuid_w:{BitConverter.ToString(uidWrited ?? new byte[] {})}\tSerialNumberSize:{SerialNumberSize}\tSerialNumberSize_writed:{serialNumberSizeWrited}\t";
                    WriteToLog(text);
                    return (int) ErrorCodes.E_SUCCESS;
                }
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Anticollision ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_GENERIC;
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
                    $"!!! SelectCard over !!!\tobj:{Obj}\tSerialNumber:{SerialNumber}\tSerialNumberSize:{SerialNumberSize}\tuid:{BitConverter.ToString(uid ?? new byte[] {})}\t";
                WriteToLog(text);
                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"SelectCard ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int) ErrorCodes.E_GENERIC;
            }
        }

        // Аутентификация в секторе
        //   Obj - ссылка на объект считывателя
        //   Sector - номер сектора
        //   KeyType - тип ключа (KEY_TYPE_A/KEY_TYPE_B)
        //   NonvolatileMemory - аутентификация ключа из энергонезависимой памяти
        //   KeyIndex - номер ключа (KEY_SLOT_0 + n)
        [Obfuscation]
        public static int Authentication(IntPtr Obj, int Sector, int KeyType_int, bool NonvolatileMemory, int KeyIndex)
        {
            string text =
                $"!!! Authentication !!!\tobj:{Obj}\tSector:{Sector}\tKeyType:{KeyType_int}\tNonvolatileMemory:{NonvolatileMemory}\tKeyIndex:{KeyIndex}\t";
            WriteToLog(text);
            try
            {
                if (_reader == null || _card == null)
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;

                var keyA = GetKeyFromCollection(Sector, 0);
                var keyB = GetKeyFromCollection(Sector, 1);

                if (/*KeyType_int == 0 &&*/ keyA != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyA, Sector = Sector, Key = keyA });
                if (/*KeyType_int == 1 &&*/ keyB != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet { KeyType = KeyType.KeyB, Sector = Sector, Key = keyB });
/*
                var sec = _card.GetSector(Sector);

                //TODO только для тестов!
                if (_cardBadSectors?.Contains(new Tuple<int, int>(Sector, sec.NumDataBlocks -  1))??false)
                    return (int) ErrorCodes.E_SUCCESS;

                var secAuthentification = sec.GetData(0).Result;
                if (secAuthentification == null)
                    return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
*/
                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"Authentication ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
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
                if (_card != null)
                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(Block, out controlSector, out blockInSector);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! ReadBlock before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    //TODO удалить
                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector))??false)
                    {
                        byte[] dataBadSectors = ReadFromFileSector(controlSector, blockInSector);

                        if (dataBadSectors.Length > 0 && dataBadSectors.Length < 17)
                        {
                            WriteToLog($"SaveInMemory from file {dataBadSectors.Length} byte");
                            UnMemory<byte>.SaveInMemArr(dataBadSectors, ref Buffer);
                        }

                        //TODO убрать логи чтения и записи
                        //WriteToLog($"Sector '{controlSector}':[{blockInSector}]{dataBadSectors.ByteArrayToString()}");
                        //WriteDataToLog(controlSector, blockInSector, dataBadSectors.ByteArrayToString(), false);
                        return (int) ErrorCodes.E_SUCCESS;
                    }

                    byte[] data = GetData(controlSector, blockInSector, keyA, keyB).Result;
                    if (data.Length > 0 && data.Length < 17)
                    {
                        WriteToLog($"SaveInMemory {data.Length} byte");
                        UnMemory<byte>.SaveInMemArr(data, ref Buffer);

                        //TODO убрать логи чтения и записи
                        //WriteToLog($"Sector '{controlSector}':[{blockInSector}]{data.ByteArrayToString()}");
                        //WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), false);

                        return (int)ErrorCodes.E_SUCCESS;
                    }
                    return (int) ErrorCodes.E_GENERIC;
                }
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"ReadBlock ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }
        private static async Task<byte[]> GetData(int sector, int blockInSector, byte[] keyA, byte[] keyB)
        {
            byte[] result = {};
            try
            {
                if (keyA != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = keyA });
                if (keyB != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = sector, Key = keyB });

                var sec = _card.GetSector(sector);
                result = await sec.GetData(blockInSector);
            }
            catch (Exception e)
            {
                WriteToLog($"GetData ERROR !!! {e.ToString()}", true);
            }
            return result;
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
                if (_card != null)
                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(Block, out controlSector, out blockInSector);

                    byte[] data = UnMemory<byte>.ReadInMemArr(Data, 16);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! WriteBlock internal !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}\tData:{BitConverter.ToString(data ?? new byte[] { })}\t";
                    WriteToLog(text);

                    //TODO убрать
                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                    {
                        WriteOrReplaceToFileSector(controlSector, blockInSector, data);
                        text = "!!! WriteBlock to file!";
                        WriteToLog(text);
                        return (int)ErrorCodes.E_SUCCESS;
                    }

                    //WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), true);

                    var result = SetData(data, controlSector, blockInSector, keyA, keyB).Result;
                    if (result == (int) ErrorCodes.E_SUCCESS)
                        result = Transfer(Obj, Block);

                    return result;
                }
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"WriteBlock ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }
        private static async Task<int> SetData(byte[] data, int sector, int blockInSector, byte[] keyA, byte[] keyB)
        {
            if (data == null)
                throw new Exception("!!!! SetData ERROR !!! data null");

            try
            {
                if (keyA != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = keyA });
                if (keyB != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = sector, Key = keyB });

                var sec = _card.GetSector(sector);
                if (blockInSector == sec.NumDataBlocks - 1)
                {
                    var access = AccessBits.GetAccessConditions(data);
                    sec.Access.DataAreas[0] = access.DataAreas[0];
                    sec.Access.DataAreas[1] = access.DataAreas[1];
                    sec.Access.DataAreas[2] = access.DataAreas[2];
                    sec.Access.Trailer = access.Trailer;
                    byte[] keyAData = new byte[6];
                    byte[] keyBData = new byte[6];
                    Array.Copy(data, 0, keyAData, 0, 6);
                    Array.Copy(data, 10, keyBData, 0, 6);
                    sec.KeyA = keyAData.ByteArrayToString();
                    sec.KeyB = keyBData.ByteArrayToString();
                }
                else
                    await sec.SetData(data, blockInSector);

                return (int) ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"SetData ERROR !!! {e.ToString()}", true);
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
                if (_card != null)
                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(Block, out controlSector, out blockInSector);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! Decrement before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                    {
                        byte[] dataBadSectors = ReadFromFileSector(controlSector, blockInSector);
                        IncDecByteArray(ref dataBadSectors, -Value);
                        WriteOrReplaceToFileSector(controlSector, blockInSector, dataBadSectors);
                        //TODO убрать логи чтения и записи
                        //WriteToLog($"Decremented sector in file '{controlSector}':[{blockInSector}]{dataBadSectors.ByteArrayToString()}");
                        //WriteDataToLog(controlSector, blockInSector, dataBadSectors.ByteArrayToString(), true);

                        return (int)ErrorCodes.E_SUCCESS;
                    }

                    byte[] data = GetData(controlSector, blockInSector, keyA, keyB).Result;
                    IncDecByteArray(ref data, -Value);
                    var result = SetData(data, controlSector, blockInSector, keyA, keyB).Result;

                    if (result == (int)ErrorCodes.E_SUCCESS)
                        result = Transfer(Obj, Block);

                    //TODO убрать логи чтения и записи
                    //WriteToLog($"Decremented sector '{controlSector}':[{blockInSector}]{data.ByteArrayToString()}");
                    //WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), true);

                    return result;
                }
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Decrement ERROR !!! {text}\r\n {e.ToString()}", true);
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
                if (_card != null)
                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(Block, out controlSector, out blockInSector);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! Increment before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                    {
                        byte[] dataBadSectors = ReadFromFileSector(controlSector, blockInSector);
                        IncDecByteArray(ref dataBadSectors, Value);
                        WriteOrReplaceToFileSector(controlSector, blockInSector, dataBadSectors);
                        //TODO убрать логи чтения и записи
                        //WriteToLog($"Incremented sector in file '{controlSector}':[{blockInSector}]{dataBadSectors.ByteArrayToString()}");
                        //WriteDataToLog(controlSector, blockInSector, dataBadSectors.ByteArrayToString(), true);

                        return (int)ErrorCodes.E_SUCCESS;
                    }

                    byte[] data = GetData(controlSector, blockInSector, keyA, keyB).Result;
                    IncDecByteArray(ref data, Value);
                    var result = SetData(data, controlSector, blockInSector, keyA, keyB).Result;

                    if (result == (int)ErrorCodes.E_SUCCESS)
                        result = Transfer(Obj, Block);

                    //TODO убрать логи чтения и записи
                    //WriteToLog($"Incremented sector '{controlSector}':[{blockInSector}]{data.ByteArrayToString()}");
                    //WriteDataToLog(controlSector, blockInSector, data.ByteArrayToString(), true);

                    return result;
                }
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Increment ERROR !!! {text}\r\n {e.ToString()}", true);
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
                if (_card != null)
                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(Block, out controlSector, out blockInSector);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! Restore before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    if (keyA != null)
                        _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = controlSector, Key = keyA });
                    if (keyB != null)
                        _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = controlSector, Key = keyB });

                    var sec = _card.GetSector(controlSector);

                    //TODO только для тестов!
                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector))??false)
                        return (int)ErrorCodes.E_SUCCESS;

                    int result = (int)ErrorCodes.E_SUCCESS;
                    if (sec.RestoreData(blockInSector).Result)
                        result = Transfer(Obj, Block);

                    return result;
                }
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Restore ERROR !!! {text}\r\n {e.ToString()}", true);
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
                if (_card != null)
                {
                    int blockInSector;
                    int controlSector;
                    BlockToSectorBlock(Block, out controlSector, out blockInSector);

                    var keyA = GetKeyFromCollection(controlSector, 0);
                    var keyB = GetKeyFromCollection(controlSector, 1);

                    text =
                        $"!!! Transfer before !!!\tcurrentSector: {controlSector}\tblockInSector: {blockInSector}\tkeyA: {BitConverter.ToString(keyA ?? new byte[] { })}\tkeyB: {BitConverter.ToString(keyB ?? new byte[] { })}";
                    WriteToLog(text);

                    //TODO только для тестов!
                    if (_cardBadSectors?.Contains(new Tuple<int, int>(controlSector, blockInSector)) ?? false)
                        return (int)ErrorCodes.E_SUCCESS;

                    return FlushData(controlSector, blockInSector, keyA, keyB).Result;
                }
                return (int)ErrorCodes.E_CARDREADER_NOT_INIT;
            }
            catch (Exception e)
            {
                WriteToLog($"Transfer ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }
        private static async Task<int> FlushData(int sector, int blockInSector, byte[] keyA, byte[] keyB)
        {
            try
            {
                if (keyA != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = keyA });
                if (keyB != null)
                    _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = sector, Key = keyB });

                var sec = _card.GetSector(sector);
                await sec.Flush();
                if (blockInSector == sec.NumDataBlocks - 1)
                {
                    if (keyA != null)
                        _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyA, Sector = sector, Key = keyA });
                    if (keyB != null)
                        _card.AddOrUpdateSectorKeySet(new SectorKeySet() { KeyType = KeyType.KeyB, Sector = sector, Key = keyB });

                    if (keyA != null && keyB != null)
                        await sec.FlushTrailer(keyA?.ByteArrayToString(), keyB?.ByteArrayToString());
                }
                return (int)ErrorCodes.E_SUCCESS;
            }
            catch (Exception e)
            {
                WriteToLog($"FlushData ERROR !!! \r\n {e.ToString()}", true);
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
                WriteToLog($"Halt ERROR !!! {text}\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        private static int write_count = 0;
        private static void WriteToLog(string text, bool showMsg = false)
        {
            bool logIsError = text.Contains("ERROR");
            bool writeToLog = logIsError;
#if DEBUG
            writeToLog = true;
#endif

            if (!writeToLog)
                return;

            string path = @"C:\MifareServio\Out\DLL_Log.txt";
            if (write_count == 0 || !File.Exists(path))
            {
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
            ++write_count;
        }

        private static int write_data_count = 0;
        private static void WriteDataToLog(int sector, int block, string data, bool write)
        {
            bool writeToLog = false;
#if DEBUG
            writeToLog = true;
#endif

            if (!writeToLog)
                return;

            string path = @"C:\MifareServio\Out\DLL_Data_Log.txt";
            if (write_data_count == 0 || !File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.Write($"{(write ? "write" : " read")}: {sector} [{block}]\t{DateTime.Now.ToString()}: {data}\r\n");
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.Write($"{(write ? "write" : " read")}: {sector} [{block}]\t{DateTime.Now.ToString()}: {data}\r\n");
                }
            }
            ++write_data_count;
        }

        private static void WriteOrReplaceToFileSector(int sector, int block, byte[] data)
        {
            //bool logIsError = text.Contains("ERROR!!!");
            //bool writeToLog = logIsError;
            //#if DEBUG
            //        writeToLog = true;
            //#endif

            //if (!writeToLog)
            //    return;

            var uid = _card?.GetUid().Result;
            if (uid == null)
                throw new Exception("Попытка записи данных в неинициализированную карты");

            string path = @"C:\MifareServio\Out\file_sector_" + uid.ByteArrayToString() + ".dat";
            if (!File.Exists(path))
            {
                using (var f = File.CreateText(path))
                {
                }
            }
            var fileList = File.ReadAllLines(path).ToList();
            var ind = fileList.FindIndex(s => s.Contains($"{sector},{block}:"));
            if (ind >= 0)
            {
                fileList[ind] = $"{sector},{block}:{data?.ByteArrayToString()}";
                File.WriteAllLines(path, fileList);
            }
            else
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.Write($"{sector},{block}:{data?.ByteArrayToString()}{Environment.NewLine}");
                }
        }
        private static byte[] ReadFromFileSector(int sector, int block)
        {
            //bool logIsError = text.Contains("ERROR!!!");
            //bool writeToLog = logIsError;
            //#if DEBUG
            //        writeToLog = true;
            //#endif

            //if (!writeToLog)
            //    return;

            var uid = _card?.GetUid().Result;
            if (uid == null)
                throw new Exception("Попытка чтения данных с неинициализированной карты");

            string path = @"C:\MifareServio\Out\file_sector_" + uid.ByteArrayToString() + ".dat";
            var sectors = new Dictionary<Tuple<int, int>, byte[]>();
            if (File.Exists(path))
            {
                var fileLise = File.ReadAllLines(path).ToList();
                fileLise.ForEach(s =>
                {
                    var arr = s.Split(':', ',');
                    if (arr.Length == 3)
                    {
                        int sec = Convert.ToInt32(arr[0]);
                        int bl = Convert.ToInt32(arr[1]);
                        var dat = arr[2].StringToByteArray();
                        sectors[new Tuple<int, int>(sec, bl)] = dat;
                    }
                });
            }

            byte[] data;
            return sectors.TryGetValue(new Tuple<int, int>(sector, block), out data) ? data : null;
        }
        private static HashSet<Tuple<int, int>> ReadBadsFromFileSector()
        {
            //bool logIsError = text.Contains("ERROR!!!");
            //bool writeToLog = logIsError;
            //#if DEBUG
            //        writeToLog = true;
            //#endif

            //if (!writeToLog)
            //    return;

            var uid = _card?.GetUid().Result;
            if (uid == null)
                throw new Exception("Попытка чтения данных с неинициализированной карты");

            string path = @"C:\MifareServio\Out\file_sector_" + uid.ByteArrayToString() + ".dat";

            var result = new HashSet<Tuple<int, int>>();

            if (File.Exists(path))
            {
                using (var file = new StreamReader(path))
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
                WriteToLog($"InitializeReader ERROR!!!\r\n {e.ToString()}", true);
                return (int)ErrorCodes.E_GENERIC;
            }
        }

        private static void Reader_CardRemoved(object sender, CardRemovedEventArgs ev)
        {
            WriteToLog("Reader_CardRemoved");
            try
            {
                _card?.Dispose();
                _card = null;
                _cardBadSectors?.Clear();
                _keys?.Clear();
                _isInfoNoCardShow = false;
            }
            catch (Exception e)
            {
                WriteToLog($"Reader_CardRemoved ERROR!!!\r\n {e.ToString()}", true);
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
                _cardBadSectors = ReadBadsFromFileSector();
                _isInfoNoCardShow = false;
            }
            catch (Exception e)
            {
                WriteToLog($"Reader_CardAdded ERROR!!!\r\n {e.ToString()}", true);
                throw e;
            }
        }

        private static async void InitCard()
        {
            WriteToLog("InitCard");
            try
            {
                if (_card == null)
                {
                    if (_isInfoNoCardShow)
                        return;

                    MessageBox.Show("No card!");
                    _isInfoNoCardShow = true;

                    return;
                }

                _cardIdentification = await _card.GetCardInfo();
                WriteToLog("Connected to card\r\nPC/SC device class: " + _cardIdentification.PcscDeviceClass.ToString() +
                           "\r\nCard name: " + _cardIdentification.PcscCardName.ToString());
            }
            catch (Exception e)
            {
                WriteToLog("InitCard ERROR!!!\r\n" + e.Message);
                throw e;
            }
        }

        private static void BlockToSectorBlock(int block, out int sector, out int blockInSector)
        {
            //32 сектора по 4 блока
            int blocksSize4 = 32 * 4;
            int dataBlockSize = block < blocksSize4 ? 4 : 16;
            sector = (block < blocksSize4) ? (block/dataBlockSize) : ((block - blocksSize4)/dataBlockSize + 32);
            blockInSector = (block < blocksSize4 ? block : block - blocksSize4) % dataBlockSize;//TODO  ???????????
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
            byte[] key;
            if (_keys.TryGetValue(new Tuple<int, int>(sector, keyType), out currentKey))
            {
                key = currentKey.Item3;
                return key;
            }

            return null;
        }
    }
}