using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
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
using Microsoft.Data.ConnectionUI;
using Microsoft.SqlServer.Dac;

namespace SqlDacUIPublisher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_ConnectionString(object sender, RoutedEventArgs e)
        {
            // Display the connection dialog
            var dlg = new DataConnectionDialog();
            dlg.DataSources.Add(DataSource.SqlDataSource);
            dlg.DataSources.Add(DataSource.SqlFileDataSource);

            if (System.Windows.Forms.DialogResult.OK == DataConnectionDialog.Show(dlg))
            {
                ConnectionStringTextBox.Text = dlg.ConnectionString;
            }
        }

        private void DacpacButton_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    DacpacTextBox.Text = file;
                    DacpacTextBox.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DacpacTextBox.IsEnabled = false;
                ConnectionStringTextBox.IsEnabled = false;
                PublishButton.IsEnabled = false;
                PublishButton.Content = "Publishing...";
                ResultLabel.Content = "Result: ";
                ResultTextBox.Text = string.Empty;

                if (!File.Exists(DacpacTextBox.Text)) throw new ArgumentException("The dacpac file does not exists");
                TestConnection(ConnectionStringTextBox.Text);

                var dac = DacPackage.Load(DacpacTextBox.Text);
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionStringTextBox.Text);
                var dacService = new DacServices(ConnectionStringTextBox.Text);

                var result = await Task.Run<PublishResult>(() => dacService.Publish(dac, builder.InitialCatalog, new PublishOptions()));
                ResultTextBox.Text = result.DatabaseScript;
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = ex.ToFormattedString();
                ResultLabel.Content = "Error: ";
            }
            finally
            {
                DacpacTextBox.IsEnabled = true;
                ConnectionStringTextBox.IsEnabled = true;
                PublishButton.IsEnabled = true;
                PublishButton.Content = "Publish";
            }
        }

        private void TestConnection(string connectionString)
        {
            string provider = "System.Data.SqlClient";
            DbProviderFactory factory = DbProviderFactories.GetFactory(provider);
            using (DbConnection conn = factory.CreateConnection())
            {
                try
                {
                    conn.ConnectionString = connectionString;
                    conn.Open();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Connection is not working", ex);
                }
            }
        }
    }
}

