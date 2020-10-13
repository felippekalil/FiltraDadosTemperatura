using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace FiltraDadosTemperatura
{
    public partial class Form1 : Form
    {
        private struct Temperaturas
        {
            public double Tempo; 
            public float[] Temp;
        }

        private readonly bool[] _habCanal = new bool[12];

        private readonly List<Temperaturas> _lista = new List<Temperaturas>();
        private double taxaOriginal = 0;
        private string nomeArquivo;

        public Form1()
        {
            InitializeComponent();
            AtualizaCbs();
        }

        private void AtualizaCbs()
        {
            for (var i = 0; i < 12; i++)
                _habCanal[i] = ((CheckBox) Controls.Find("checkBox" + (i + 1), true)[0]).Checked;
        }

        private void BtAbrir_Click(object sender, EventArgs e)
        {
            var openFile = new OpenFileDialog
            {
                Multiselect = false,
                Filter = @"Arquivo de texto|*.txt|Todos Arquivos|*.*",
                FilterIndex = 0,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                nomeArquivo = openFile.FileName;
                var file = new StreamReader(nomeArquivo);
                var texto = file.ReadToEnd();
                file.Close();
                var linhas = texto.Split('\n');
                AtualizaLista(linhas);
            }
        }

        private void AtualizaLista(IReadOnlyList<string> linhas)
        {
            _lista.Clear();
            for (var i = 0; i < linhas.Count(); i++)
            {
                var temp = new Temperaturas { Temp = new float[12] };
                var linha = linhas[i];
                linha = linha.Replace('\r', ' ');
                var dado = linha.Split(' ');
                if (dado.Length < 26)
                    break;
                temp.Tempo = Convert.ToDouble(dado[0]);
                for (var j = 0; j < 12; j++)
                    temp.Temp[j] = Convert.ToSingle(dado[(j + 1) * 2]);
                _lista.Add(temp);
            }

            if (_lista.Count > 1)
                taxaOriginal = 1 / (_lista[1].Tempo - _lista[0].Tempo);
            lbTempoOriginal.Text = @"Taxa Amostragem Original: " + taxaOriginal.ToString("F1") + @" Hz";
            AtualizaGraph();
        }

        private static void MinMax(ref double minimum, ref double maximum, int decimais = 1)
        {
            var min = Math.Round(minimum, decimais);
            var max = Math.Round(maximum, decimais++);
            while (min == max)
            {
                min = Math.Round(minimum, decimais);
                max = Math.Round(maximum, decimais++);
            }
            minimum = min;
            maximum = max;
        }

        private void AtualizaGraph()
        {
            foreach (var series in chartTemperaturas.Series)
                series.Points.Clear();

            if (_lista.Count == 0)
                return;

            var nValues = 1000;
            if (_lista.Count < nValues)
                nValues = _lista.Count;
            var offset = _lista.Count / nValues;
            for (var i = 0; i < _lista.Count; i+=offset)
            for (var j = 0; j < 12; j++)
                if (_habCanal[j])
                    chartTemperaturas.Series[j].Points.AddXY(_lista[i].Tempo, _lista[i].Temp[j]);

            chartTemperaturas.ChartAreas[0].AxisX.Minimum = 0;
            chartTemperaturas.ChartAreas[0].AxisX.Maximum = _lista.Max(pontos => pontos.Tempo);

            chartTemperaturas.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartTemperaturas.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            var min = chartTemperaturas.ChartAreas[0].AxisX.Minimum;
            var max = chartTemperaturas.ChartAreas[0].AxisX.Maximum;
            MinMax(ref min, ref max);
            chartTemperaturas.ChartAreas[0].AxisX.Minimum = min;
            chartTemperaturas.ChartAreas[0].AxisX.Maximum = max;
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AtualizaCbs();
            AtualizaGraph();
        }

        private void btSalvar_Click(object sender, EventArgs e)
        {
            btSalvar.Enabled = false;
            var saveFileDialog = new SaveFileDialog()
            {
                InitialDirectory =  Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                FileName = "DadoFiltrado.txt",
                Filter = @"Arquivo de texto|*.txt|Todos Arquivos|*.*",
                FilterIndex = 0
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var file = new StreamWriter(saveFileDialog.FileName);
                file.Write("");
                var offset = (int)(taxaOriginal / (double)numericUpDown1.Value);
                for (var i = 0; i < _lista.Count; i += offset)
                {
                    var linha = _lista[i].Tempo.ToString();
                    for (var j = 0; j < 12; j++)
                        if (_habCanal[j])
                            linha += "\t" + _lista[i].Temp[j];
                    file.WriteLine(linha);
                }
                file.Close();
            }

            btSalvar.Enabled = true;
        }
    }
}
