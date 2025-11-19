using System.ComponentModel;

namespace DevCardsManager.Models;

public enum CardModel
{
    /*
     Модель карт            Общий объем памяти  Размер UID (байт) Примечания
     MIFARE Classic 1K      1024 байт (1 КБ)    4                 Стандартная карта. UID постоянный и не может быть изменен.
     MIFARE Classic 4K      4096 байт (4 КБ     4 или 7           Имеет 4-байтный UID, но некоторые версии (например, Plus 4K) поддерживают 7-байтный.
     MIFARE Plus 2K         2048 байт (2 КБ     4 или 7           Более безопасная замена Classic. Поддерживает оба размера UID.
     MIFARE Plus 4K         4096 байт (4 КБ     4 или 7           Аналог Classic 4K с улучшенной криптографией.
     MIFARE Ultralight      64 байта            7                 Бюджетная карта для одноразовых применений. Часто используется в NFC-билетах.
     MIFARE Ultralight C    192 байта           7                 Версия Ultralight с шифрованием (3DES).
     MIFARE Ultralight EV1  64 или 192 байта    7                 Улучшенная версия с большим объемом памяти и функциями защиты.
     MIFARE DESFire EV2 2K  2048 байт (2 КБ)    7                 Карта высокого класса с файловой системой и продвинутой безопасностью.
     MIFARE DESFire EV3 4K  4096 байт (4 КБ)    7                 Флагманская модель с наибольшей памятью и безопасностью.
    */

    Unknown,
    [Description("MIFARE Classic 1K")]
    MifareClassic1K,
    [Description("MIFARE Classic 4K")]
    MifareClassic4K,
    [Description("MIFARE Plus 2K")]
    MifarePlus2K,
    [Description("MIFARE Plus 4K")]
    MifarePlus4K,
    [Description("MIFARE Ultralight")]
    MifareUltralight,
    [Description("MIFARE Ultralight C")]
    MifareUltralightC,
    [Description("MIFARE Ultralight EV1")]
    MifareUltralightEv1
}