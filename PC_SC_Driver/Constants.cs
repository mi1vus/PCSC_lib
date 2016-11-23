﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoverConstants
{
    public enum ErrorCodes
    {
        // Коды ошибок терминала.

        // Здесь определены ошибки, которые использует терминал оффлайн-карт Servio.
        // Любой другой код ошибки, отличный от нуля будет означать ошибку на
        // стороне других разработчиков, системную или OLE-ошибку.

        // 3.0.0.363
        E_SUCCESS                     =   0, // Операция выполнена успешно
        E_CANCEL                      = 200, // Ожидание карты отменено оператором
        E_GENERIC                     = 229, // Необрабатываемая ошибка 1 (Принзнак исключения)
        E_POS_KEYS_LOAD               = 254, // Ошибка при загрузке ключей для обслуживания карты
        E_CHECKSUM                    = 240, // Неверная контрольная сумма карты. Возможно данные карты испорчены ! Операция не возможна !
        E_DEMO                        = 227, // Превышение ограничений демонстрационной версии
        E_PRODUCT_TABLE               = 235, // Не верно настроена таблица соответствий продуктов
        E_CARD_TYPE                   = 237, // Не верный тип карты !
        E_CARD_DISALLOWED             = 236, // Карта запрещена !
        E_INVALID_PIN                 = 238, // Неверный PIN-код !
        E_CARD_CHANGED                = 234, // Текущая карта не соответствует авторизованной
        E_PRODUCT_NOT_FOUND           = 249, // На карте нет данного продукта или настройки кошельков и продуктов не соотвествуют структуре карты !
        E_LAST_PAYMENT_DATE           = 250, // Дата последней операции с картой больше текушей ! Проверьте даты !
        E_ORDER_LIMIT                 = 251, // Превышен лимит заказа карты ! Операция не возможна !
        E_BALANCE                     = 253, // Исчерпан баланс ! Операция не возможна !
        E_DAY_LIMIT                   = 252, // Превышен дневной лимит карты! Операция не возможна !
        E_MONTH_LIMIT                 = 246, // Превышен месячный лимит карты ! Операция не возможна !
        E_AMOUNT_OVERFLOW             = 244, // Баланс карты достиг предельного размера ! Операция не возможна ! Обратитесь в процессинговый центр !
        E_RETURN_GREATER_SALE         = 248, // Возврат превышает последнюю заказанную сумму ! Операция не возможна !

        // 3.0.0.746
        E_GLOBAL_ISSUER_NOT_FOUND     = 501, // Эмитент не найден в базе данных по глобальному коду
        E_LOCAL_ISSUER_NOT_FOUND      = 502, // Эмитент не найден в базе данных по локальному коду
        E_LOAD_KEYS_FROM_DB           = 503, // Ошибка загрузки ключей из базы данных !
        E_CARD_EXPIRED                = 510, // Истек срок действия карты !

        // 3.0.0.747
        E_UNIT_ID                     = 511, // Выбранная единица списания не поддерживается в данной реализации
        E_WAIT_TIMEOUT                = 512, // Вышел таймаут ожидания карты
        E_BUSY                        = 513, // Терминал занят
        E_MODEL                       = 514, // Выбранная модель считывателя не поддерживается в данной реализации
        E_BOS_KEYS_LOAD               = 515, // Ошибка при загрузке ключей для выпуска карт
        E_AMOUNT_UNDERFLOW            = 516, // При расчетах происходит потеря точности в регистре карты. Карту следует перевыпустить или проверить баланс в офисе.
        E_RETURN_GREATER_ORDER_LIMIT  = 517, // Сумма к возврату превышает лимит заказа !
        E_RETURN_GREATER_DAY_AMOUNT   = 518, // Сумма к возврату превышает сумму за сутки !
        E_RETURN_GREATER_MONTH_AMOUNT = 519, // Сумма к возврату превышает сумму за месяц !
        E_CARDREADER_NOT_INIT         = 520, // Внутренний объект считывателя не инициализирован
        E_DB_NOT_INIT                 = 521, // Внутренний объект базы данных не инициализирован
        E_OP_WO_CARD                  = 522, // Операция не может проводится без карты
        E_MULTIISSUER_NOT_CONVERTED   = 523, // Карта не конвертирована для работы в мультиэмитентной системе. Перед обслуживанием клиенту следует обратится в офис, выпустивший карту.
        //E_MAX_OPERATION_PURSES        = 524, // Не все пополнения применены
        E_TEMPLATE_NOT_FOUND          = 525, // Не удалось загрузить файл шаблона квитанций !
        E_ISSUER_NOT_FOUND            = 526, // Эмитент не найден в базе данных!
        E_LOAD_KEYS_FROM_FILE         = 527, // Ошибка загрузки ключей из файла !

        // 3.0.0.752
        E_REPLACE_KEYS                = 528, // Ошибка замены ключей !
        E_EXPECTED_BALANCE            = 529, // Баланс на карте больше ожидаемого !
        E_EXPECTED_OVERDRAFT          = 530, // Флаг овердрафта на карте не сходится со значением в базе !
        E_EXPECTED_DAY_LIMIT          = 531, // Дневной лимит на карте больше ожидаемого !
        E_EXPECTED_MONTH_LIMIT        = 532, // Месячный лимит на карте больше ожидаемого !
    }
}
