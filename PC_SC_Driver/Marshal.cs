using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MarshalHelper
{
    /// <summary>
    /// Класс для очистки блоков неуправляемой памяти
    /// </summary>
    public static class UnMemory
    {
        /// <summary>
        /// Очередь для высвобождения блоков памяти
        /// </summary>
        private static Queue<IntPtr> queue = new Queue<IntPtr>();

        public static void Enqueue(IntPtr ptr)
        {
            queue.Enqueue(ptr);
        }

        private static void FreeIntPtr(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(ptr);
        }

        /// <summary>
        /// Освобождение блоков памяти в неуправляемом пространстве
        /// </summary>
        public static void FreeMemory()
        {
            while (queue.Count > 0)
            {
                IntPtr temp = queue.Dequeue();
                // освобождаем то, что записано в памяти
                Marshal.FreeCoTaskMem(temp);
            }
        }
    }

    /// <summary>
    /// Класс для работы неуправляемой памятью
    /// </summary>
    /// <typeparam name="T">Структурный тип данных</typeparam>
    public static class UnMemory<T>
      where T : struct
    {

        /// <summary>
        /// Получить указатель на структуру в неуправляемом куску памяти
        /// </summary>
        /// <param name="memory_object">Объект для сохранения</param>
        /// <param name="ptr">Указатель</param>
        /// <typeparam name="T">Структурный тип данных</typeparam>
        public static void SaveInMem(T memory_object, ref IntPtr ptr)
        {
            if (default(T).Equals(memory_object))
            {
                // объявляем указатель на кусок памяти
                ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(T)));
                UnMemory.Enqueue(ptr);
                return;
            }

            if (ptr == IntPtr.Zero)
            {
                // объявляем указатель на кусок памяти
                ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(T)));

                // записываем в память данные структуры
                Marshal.StructureToPtr(memory_object, ptr, false);
            }
            else
            {
                // записываем в память данные структуры
                Marshal.StructureToPtr(memory_object, ptr, true);
            }

            UnMemory.Enqueue(ptr);
        }

        /// <typeparam name="T">IntPtr, int, float</typeparam>
        /// <exception cref="System.ArgumentException">Параметр #1 должен быть массивом IntPtr, int, float</exception>
        public static void SaveInMemArr(T[] managedArray, ref IntPtr pnt, int length = 0)
        {
            Debug.Assert(managedArray != null, "Объект не должен быть Null");
            Debug.Assert(managedArray.Length != 0, "Объект не может иметь длину массива 0");

            if (length == 0)
                length = managedArray.Length;

            if (pnt == IntPtr.Zero)
            {
                // объявляем указатель на кусок памяти. Размер = размер одного элемента * количество
                //int size = Marshal.SizeOf(typeof(T)) * managedArray.Length;
                int size = Marshal.SizeOf(managedArray[0]) * managedArray.Length;
                pnt = Marshal.AllocCoTaskMem(size);
            }

            // в зависимости от типа массива, мы вызываем соответствующий метод в Marshal.Copy
            if (typeof(T) == typeof(int))
            {
                int[] i = managedArray as int[];
                Marshal.Copy(i, 0, pnt, Math.Min(i.Length, length));
            }
            else if (typeof(T) == typeof(byte))
            {
                byte[] b = managedArray as byte[];
                Marshal.Copy(b, 0, pnt, Math.Min(b.Length, length));
            }
            else if (typeof(T) == typeof(float))
            {
                float[] f = managedArray as float[];
                Marshal.Copy(f, 0, pnt, Math.Min(f.Length, length));
            }
            else if (typeof(T) == typeof(char))
            {
                // читаем массив байтов и переводим в текущую кодировку
                byte[] b = Encoding.Default.GetBytes(managedArray as char[]);
                Marshal.Copy(b, 0, pnt, b.Length);
            }
            else if (typeof(T) == typeof(IntPtr))
            {
                IntPtr[] p = managedArray as IntPtr[];
                Marshal.Copy(p, 0, pnt, Math.Min(p.Length, length));
            }
            else
                throw new ArgumentException("Параметр #1 должен быть массивом IntPtr, int, float или char");

            // запоминаем указатель, чтобы потом его почистить
            UnMemory.Enqueue(pnt);
        }

        /// <summary>
        /// Чтение структуры из неуправляемой памяти
        /// </summary>
        /// <param name="ptr">Указатель</param>
        /// <param name="type">Тип данных для чтения</param>
        /// <returns>Структура из памяти</returns>
        public static T ReadInMem(IntPtr ptr)
        {
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }

        public static T[] ReadInMemArr(IntPtr ptr, int size)
        {
            if (typeof(T) == typeof(int))
            {
                int[] memInt = new int[size];
                Marshal.Copy(ptr, memInt, 0, size);
                return memInt as T[];
            }
            else if (typeof(T) == typeof(byte))
            {
                byte[] memByte = new byte[size];
                Marshal.Copy(ptr, memByte, 0, size);
                return memByte as T[];
            }
            else if (typeof(T) == typeof(float))
            {
                float[] memFloat = new float[size];
                Marshal.Copy(ptr, memFloat, 0, size);
                return memFloat as T[];
            }
            else if (typeof(T) == typeof(IntPtr))
            {
                IntPtr[] memIntPtr = new IntPtr[size];
                Marshal.Copy(ptr, memIntPtr, 0, size);
                return memIntPtr as T[];
            }
            else
                throw new ArgumentException("Параметр #1 должен быть массивом int, float или char");
        }

        /// <summary>
        /// Класс переводит массивы
        /// </summary>
        public static class UnArray
        {
            /// <summary>
            /// Перевод одномерного массива в двумерный
            /// </summary>
            /// <typeparam name="T">Тип исходного массива</typeparam>
            /// <param name="array">Исходный массив</param>
            /// <returns>Двумерный массив</returns>
            public static T[,] Rank1_Rank2(T[] array, int x, int y)
            {
                T[,] res = new T[x, y];
                int size = Buffer.ByteLength(array);
                Buffer.BlockCopy(array, 0, res, 0, size);
                return res;
            }

            /// <summary>
            /// Перевод двумерного в одномерный массив
            /// </summary>
            /// <typeparam name="T">Тип исходного массива</typeparam>
            /// <param name="array">Исходный массив</param>
            /// <returns>Одномерный массив</returns>
            public static T[] ToRank1(T[,] array, int x, int y)
            {
                T[] res = new T[x * y];
                int size = Buffer.ByteLength(array);
                Buffer.BlockCopy(array, 0, res, 0, size);
                return res;
            }

            /// <summary>
            /// Перевод одномерного массива в трехмерный
            /// </summary>
            /// <typeparam name="T">Тип исходного массива</typeparam>
            /// <param name="array">Исходный массив</param>
            /// <returns>Трехмерный массив</returns>
            public static T[,,] Rank1_Rank3(T[] array, int x, int y, int z)
            {
                T[,,] res = new T[x, y, z];
                int size = Buffer.ByteLength(array);
                Buffer.BlockCopy(array, 0, res, 0, size);
                return res;
            }

            /// <summary>
            /// Перевод трехмерного массива в одномерный
            /// </summary>
            /// <typeparam name="T">Тип исходного массива</typeparam>
            /// <param name="array">Исходный массив</param>
            /// <returns>Одномерный массив</returns>
            public static T[] ToRank1(T[,,] array, int x, int y, int z)
            {
                T[] res = new T[x * y * z];
                int size = Buffer.ByteLength(array);
                Buffer.BlockCopy(array, 0, res, 0, size);
                return res;
            }

            /// <summary>
            /// Перевод одномерного массива в четырехмерный
            /// </summary>
            /// <typeparam name="T">Тип исходного массива</typeparam>
            /// <param name="array">Исходный массив</param>
            /// <returns>Четырехмерный массив</returns>
            public static T[,,,] Rank1_Rank4(T[] array, int x, int y, int z, int w)
            {
                T[,,,] res = new T[x, y, z, w];
                int size = Buffer.ByteLength(array);
                Buffer.BlockCopy(array, 0, res, 0, size);
                return res;
            }

            /// <summary>
            /// Перевод четырехмерного массива в одномерный
            /// </summary>
            /// <typeparam name="T">Тип исходного массива</typeparam>
            /// <param name="array">Исходный массив</param>
            /// <returns>Одномерный массив</returns>
            public static T[] ToRank1(T[,,,] array, int x, int y, int z, int w)
            {
                T[] res = new T[x * y * z * w];
                int size = Buffer.ByteLength(array);
                Buffer.BlockCopy(array, 0, res, 0, size);
                return res;
            }
        }
    }
}
