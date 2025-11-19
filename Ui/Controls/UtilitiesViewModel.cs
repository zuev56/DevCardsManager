namespace DevCardsManager.Ui.Controls;

// TODO: УТИЛИТЫ
// Запоминать расположение на экране
// - Анализ лога на выявление повторяющихся строк, в т.ч. с минимальными изменениями (регулярка)
// - Мониторинг процессов, определение запущенной при разработке программы
//   - Имитация отключения/подключения устройств
//   - Определение каталога с бинами и логами в ProgramData
//   - Открытие последнего файла лога/трейса

public sealed class UtilitiesViewModel : ViewModelBase
{
    public UtilitiesViewModel(LogFilterViewModel logFilterViewModel)
    {
        LogFilterViewModel = logFilterViewModel;
    }

    public LogFilterViewModel LogFilterViewModel { get; }
}