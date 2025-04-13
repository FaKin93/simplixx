using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZLP
{
    public partial class MainWindow : Window
    {
        private TextBox[,] A; // матрица
        private TextBox[] b; // массив поставок
        private TextBox[] c; // массив спроса
        private int rows = 0; // количество рядов
        private int columns = 0; // количество столбцов

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateMatrix_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем существующие элементы управления
            tableGrid.Children.Clear();
            tableGrid.RowDefinitions.Clear(); // Очищаем существующие строки
            tableGrid.ColumnDefinitions.Clear(); // Очищаем существующие столбцы

            // Считываем значения для рядов и столбцов
            if (!int.TryParse(rowsInput.Text, out rows) || !int.TryParse(columnsInput.Text, out columns) || rows <= 0 || columns <= 0)
            {
                MessageBox.Show("Введите корректные значения для рядов и столбцов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Добавляем строки и столбцы в Grid
            for (int i = 0; i < rows + 1; i++) // +1 для правой части (b)
            {
                tableGrid.RowDefinitions.Add(new RowDefinition());
            }
            for (int j = 0; j < columns + 1; j++) // +1 для целевой функции (c)
            {
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            // Инициализируем массивы
            A = new TextBox[rows, columns];
            b = new TextBox[rows];
            c = new TextBox[columns];

            // Создаем TextBox для матрицы
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < columns; j++)
                {
                    TextBox tx = new TextBox();
                    Grid.SetRow(tx, i);
                    Grid.SetColumn(tx, j);
                    A[i, j] = tx; // Заполняем матрицу

                    tx.FontSize = 20;
                    tx.VerticalContentAlignment = VerticalAlignment.Center;
                    tx.HorizontalContentAlignment = HorizontalAlignment.Center;
                    tableGrid.Children.Add(tx);
                }

                // Добавляем TextBox для правой части (b)
                TextBox bTx = new TextBox();
                Grid.SetRow(bTx, i);
                Grid.SetColumn(bTx, columns); // Помещаем в последний столбец
                b[i] = bTx;
                bTx.Background = new SolidColorBrush(Color.FromRgb(255, 151, 187));
                bTx.FontSize = 20;
                bTx.VerticalContentAlignment = VerticalAlignment.Center;
                bTx.HorizontalContentAlignment = HorizontalAlignment.Center;
                tableGrid.Children.Add(bTx);
            }

            // Создаем TextBox для целевой функции (c)
            for (int j = 0; j < columns; j++)
            {
                TextBox cTx = new TextBox();
                Grid.SetRow(cTx, rows); // Помещаем в последний ряд
                Grid.SetColumn(cTx, j);
                c[j] = cTx;
                cTx.Background = new SolidColorBrush(Color.FromRgb(204, 204, 255));
                cTx.FontSize = 20;
                cTx.VerticalContentAlignment = VerticalAlignment.Center;
                cTx.HorizontalContentAlignment = HorizontalAlignment.Center;
                tableGrid.Children.Add(cTx);
            }
        }

        private void Solve(double[] c, double[,] A, double[] b)
        {
            int m = A.GetLength(0); // Количество ограничений
            int n = A.GetLength(1); // Количество переменных

            // Создаем таблицу симплекс-метода
            double[,] tableau = new double[m + 1, n + m + 1];

            // Заполняем целевую функцию
            for (int j = 0; j < n; j++)
                tableau[0, j] = -c[j]; // Для максимизации

            // Заполняем ограничения
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    tableau[i + 1, j] = A[i, j];
                tableau[i + 1, n + i] = 1; // Добавляем искусственную переменную
                tableau[i + 1, n + m] = b[i]; // Правая часть
            }

            // Проводим симплекс-метод
            while (true)
            {
                // Находим столбец для ввода
                int pivotCol = -1;
                for (int j = 0; j < n + m; j++)
                {
                    if (tableau[0, j] < 0)
                    {
                        pivotCol = j;
                        break;
                    }
                }

                if (pivotCol == -1) break; // Оптимальное решение найдено

                // Находим строку для выхода
                double minRatio = double.MaxValue;
                int pivotRow = -1;

                for (int i = 1; i <= m; i++)
                {
                    if (tableau[i, pivotCol] > 0)
                    {
                        double ratio = tableau[i, n + m] / tableau[i, pivotCol];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }
                if (pivotRow == -1)
                {
                    MessageBox.Show("Не удалось найти оптимальное решение, возможно, задача не имеет ограничений.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проводим преобразование для симплекс-метода
                double pivotValue = tableau[pivotRow, pivotCol];
                for (int j = 0; j <= n + m; j++)
                    tableau[pivotRow, j] /= pivotValue;

                for (int i = 0; i <= m; i++)
                {
                    if (i != pivotRow)
                    {
                        double factor = tableau[i, pivotCol];
                        for (int j = 0; j <= n + m; j++)
                            tableau[i, j] -= factor * tableau[pivotRow, j];
                    }
                }
            }
            // Выводим результаты
            string resultText = "Оптимальное решение:\n";
            for (int j = 0; j < n; j++)
            {
                bool isBasic = false;
                for (int i = 1; i <= m; i++)
                {
                    if (tableau[i, j] == 1) // Проверяем, является ли переменная базисной
                    {
                        resultText += $"x{j + 1} = {tableau[i, n + m]}\n"; // Значение базисной переменной
                        isBasic = true;
                        break;
                    }
                }
                if (!isBasic)
                {
                    resultText += $"x{j + 1} = 0\n"; // Если переменная не базисная, то её значение 0
                }
            }
            resultText += $"Максимальное значение функции: {tableau[0, n + m]}"; // Максимальное значение целевой функции
            MessageBox.Show(resultText, "Результаты", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void executeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double[,] aInt = new double[rows, columns];
                double[] bInt = new double[rows];
                double[] cInt = new double[columns];

                for (int i = 0; i < rows; ++i)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        aInt[i, j] = double.Parse(A[i, j].Text);
                    }
                }

                for (int i = 0; i < rows; i++)
                {
                    bInt[i] = double.Parse(b[i].Text);
                }
                for (int i = 0; i < columns; i++)
                {
                    cInt[i] = double.Parse(c[i].Text);
                }

                Solve(cInt, aInt, bInt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Предупреждение!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем матрицу и текстовые поля
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    A[i, j]?.Clear();
                }
                b[i]?.Clear();
            }

            for (int j = 0; j < columns; j++)
            {
                c[j]?.Clear();
            }

            result.Text = "?";
        }
    }
}
            

