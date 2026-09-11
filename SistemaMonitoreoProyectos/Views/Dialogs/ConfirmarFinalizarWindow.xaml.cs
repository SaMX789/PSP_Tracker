using System;
using System.Windows;
using System.Windows.Controls;

namespace SistemaMonitoreoProyectos.Views.Dialogs
{
    public partial class ConfirmarFinalizarWindow : Window
    {
        private readonly string _nombreProyecto;

        public ConfirmarFinalizarWindow(string nombreProyecto)
        {
            InitializeComponent();
            _nombreProyecto = nombreProyecto;
            TextoNombreRequerido.Text = _nombreProyecto;
        }

        private void TextoEntradaConfirmacion_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Se habilita únicamente si el texto coincide de forma exacta con el nombre de la actividad
            BotonFinalizar.IsEnabled = string.Equals(TextoEntradaConfirmacion.Text.Trim(), _nombreProyecto, StringComparison.Ordinal);
        }

        private void BotonCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BotonFinalizar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}