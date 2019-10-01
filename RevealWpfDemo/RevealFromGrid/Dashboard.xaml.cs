using RevealFromGrid.ViewModel;
using Infragistics.Samples.Data.Models;
using Infragistics.Sdk;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Globalization;
using System.Reflection;

namespace RevealFromGrid
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        public Dashboard()
        {


            InitializeComponent();

            this.Loaded += Dashboard_Loaded;

        }

        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Event handler to set in-memory datasource
            this.revealView1.DataSourcesRequested
                += RevealView1_DataSourcesRequested;

            // Event handler to save dashboards
            this.revealView1.SaveDashboard += RevealView1_SaveDashboard;

            // Event handler to save images
            this.revealView1.ImageExported += RevealView1_ImageExported;

            // Data provider setting
            this.revealView1.DataProvider =
                new EmbedDataProvider(this.DataContext as DashboardViewModel);

            // If the file exists load it, if not create a new dashboard
            var path = @"..\..\Dashboards\Sales.rdash";
            var revealView = new RevealView();
            RVDashboard dashboard = null; // setting null means a new dashboard creation

            if (File.Exists(path))
            {
                using (var fileStream = File.OpenRead(path))
                {
                    // Load dashboard definition
                    dashboard = await RevealUtility.LoadDashboard(fileStream);
                }
            }
            var settings = new RevealSettings(dashboard);

            if (UserInfo.permissionLevel == 0)
            {
                // Editable
                settings.CanEdit = true;
                settings.ShowMenu = true;
            }
            else
            {
                // Not editable
                settings.CanEdit = false;
                settings.ShowMenu = true;

            }
            // Other option settings
            settings.ShowChangeVisualization = true;
            settings.CanSaveAs = false; 
            settings.ShowExportImage = true;
            settings.ShowFilters = true;
            settings.ShowRefresh = true;

            //Set Maximized visualization
            //settings.MaximizedVisualization = settings.Dashboard.Visualizations.First();

            this.revealView1.Settings = settings;

            if (UserInfo.permissionLevel != 0 && dashboard is null)
            {
                // In case no edit rights and no dashboard difined
                this.revealView1.Visibility = Visibility.Collapsed;
                MessageBox.Show("No dashboard defined !!");
            }

        }

        private void RevealView1_ImageExported(object sender, ImageExportedEventArgs e)
        {
            // Save images
            using (Stream stream = new FileStream(@"..\..\Images\SavedDashboardImg.png", FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(e.Image));
                encoder.Save(stream);
            }
            MessageBox.Show("The image is saved");
            e.CloseExportDialog = false;
        }

        private void RevealView1_DataSourcesRequested(
            object sender, DataSourcesRequestedEventArgs e)
        {

            List<object> datasources = new List<object>();
            List<object> datasourceItems = new List<object>();

            // In-memory datasource
            var inMemoryDSI1 = new RVInMemoryDataSourceItem("SalesRecords");
            inMemoryDSI1.Title = "Sales Info";
            inMemoryDSI1.Description = "SalesRecords";
            datasourceItems.Add(inMemoryDSI1);

            var inMemoryDSI2 = new RVInMemoryDataSourceItem(
                "SalesAmountByProductData");
            inMemoryDSI2.Title = "Sales Amount By Product Data";
            inMemoryDSI2.Description = "SalesAmountByProductData";
            datasourceItems.Add(inMemoryDSI2);

            var inMemoryDSI3 = new RVInMemoryDataSourceItem("Top30LargeDeals");
            inMemoryDSI3.Title = "Top 30 Large Deals";
            inMemoryDSI3.Description = "Top30LargeDeals";
            datasourceItems.Add(inMemoryDSI3);

            var inMemoryDSI4 = new RVInMemoryDataSourceItem("MonthlySalesAmount");
            inMemoryDSI4.Title = "Monthly Sales Amount";
            inMemoryDSI4.Description = "MonthlySalesAmount";
            datasourceItems.Add(inMemoryDSI4);

            // Excel
            RVLocalFileDataSourceItem localExcelDatasource = new RVLocalFileDataSourceItem();
            localExcelDatasource.Uri = "local:/SampleData.xlsx";
            RVExcelDataSourceItem excelDatasourceItem = new RVExcelDataSourceItem(localExcelDatasource);
            excelDatasourceItem.Title = "Excel Data";
            datasourceItems.Add(excelDatasourceItem);

            // CSV
            RVLocalFileDataSourceItem localCsvDatasource = new RVLocalFileDataSourceItem();
            localCsvDatasource.Uri = "local:/SampleData.csv";
            RVExcelDataSourceItem csvDatasourceItem = new RVExcelDataSourceItem(localCsvDatasource);
            csvDatasourceItem.Title = "CSV data";
            datasourceItems.Add(csvDatasourceItem);


            e.Callback(new RevealDataSources(
                    null,
                    datasourceItems,
                    false));
        }

        private async void RevealView1_SaveDashboard(object sender, DashboardSaveEventArgs args)
        {
            //Save file
            var data = await args.Serialize();
            var path = @"..\..\Dashboards\Sales.rdash";
            //using (var output = File.OpenWrite($"{args.Name}.rdash"))
            using (var output = File.OpenWrite(path))
            {
                output.Write(data, 0, data.Length);
            }
            args.SaveFinished();
        }
    }

    public class EmbedDataProvider : IRVDataProvider
    {
        private DashboardViewModel vm;
        public EmbedDataProvider(DashboardViewModel _vm)
        {
            vm = _vm;
        }

        // Binding in-memory data
        public Task<IRVInMemoryData> GetData(
            RVInMemoryDataSourceItem dataSourceItem)
        {
            var datasetId = dataSourceItem.DatasetId;
            if (datasetId == "SalesAmountByProductData")
            {
                var data = vm.SalesAmountByProductData.ToList<SalesAmountByProduct>();

                return Task.FromResult<IRVInMemoryData>(new RVInMemoryData<SalesAmountByProduct>(data));
            }
            if (datasetId == "Top30LargeDeals")
            {
                var data = vm.Top30LargeDeals.ToList<Sale>();

                return Task.FromResult<IRVInMemoryData>(new RVInMemoryData<Sale>(data));
            }
            if (datasetId == "MonthlySalesAmount")
            {
                var data = vm.MonthlySalesAmount.ToList<MonthlySale>();

                return Task.FromResult<IRVInMemoryData>(new RVInMemoryData<MonthlySale>(data));
            }
            if (datasetId == "SalesRecords")
            {
                var data = vm.SalesRecords.ToList<Sale>();

                return Task.FromResult<IRVInMemoryData>(new RVInMemoryData<Sale>(data));
            }

            else
            {
                throw new Exception("Invalid data requested");
            }
        }
    }
}