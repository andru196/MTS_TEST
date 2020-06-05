using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using System.Data.Entity;

namespace MTS_TEST
{
	public partial class MainWindow : Window
	/*
	+ «Разработать десктоп приложение (WPF) с одной формой.
	+ Приложение должно получать сведения о погоде через API сервиса openweathermap.org и отображать пользователю.
	+ Приложение должно иметь собственную локальную БД SQLite для сохранения результатов.
	+ При запуске приложение отображает таблицу с уже сохранёнными данными предыдущих запросов и давать возможность выбирать записи для просмотра.
	+ Приложение должно давать пользователю возможность добавить или обновить данные по местоположению (город), которое введёт пользователь.
	+ Для одного местоположения сохраняется только одна запись, содержащая последние данные, полученные по запросу пользователя.»
	*/
	{
		string Uri;
		ApplicationContext database;

		public MainWindow()
		{
			var s = ConfigurationManager.GetSection("WeatherAPI") as System.Collections.Specialized.NameValueCollection;
			var key = s["APIKey"];
			var uri = s["Uri"];
			Uri = uri.Replace("{key}", key);
			
			InitializeComponent();

			database = new ApplicationContext();
			database.Weathers.Load();
			citiesGrid.ItemsSource = database.Weathers.Local.ToBindingList().OrderByDescending(x => x.DateTime);
		}

		/// <summary>
		/// Обновить отображаемые данные
		/// </summary>
		void UpdateGrid()
		{
			citiesGrid.ItemsSource = null;
			citiesGrid.ItemsSource = database.Weathers.Local.ToBindingList().OrderByDescending(x => x.DateTime);
		}

		/// <summary>
		/// Запросить данные по городу и вывести их
		/// </summary>
		private async void SendResponeButton_Click(object sender, RoutedEventArgs e)
		{
			((Button)sender).IsEnabled = false;
			((Button)sender).Background = Brushes.Red;

			var wheather = await GetCityWeatherDataAsync();
			((Button)sender).IsEnabled = true;
			if (wheather == null)
				return;
			var oldWeather = database.Weathers.Where(x => x.CityId == wheather.CityId).FirstOrDefault();
			if (oldWeather != null)
				oldWeather.UpdateData(wheather);
			else
				database.Weathers.Add(wheather);
			database.SaveChanges();
			UpdateGrid();
			((Button)sender).Background = Brushes.White;
		}

		/// <summary>
		/// Запросить данные по городу с сервера
		/// </summary>
		/// <returns>"Экземпляр данных</returns>
		async Task<Weather> GetCityWeatherDataAsync()
		{
			var city = CityTextBox.Text;
			var uri = Uri.Replace("{city}", city.Replace(' ', '+'));
			
			HttpResponseMessage resp = null;
			using (var clint = new HttpClient())
			{
				var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri));
				try
				{
					clint.Timeout = TimeSpan.FromSeconds(5);
					resp = await clint.SendAsync(request);
				}
				catch (TaskCanceledException ex)
				{
					var wrongName = CityTextBox.Text;
					RoutedEventHandler clearErrorOnFocus = null;
					clearErrorOnFocus = (s, ev) =>
					{
						CityTextBox.Text = wrongName;
						CityTextBox.GotFocus -= clearErrorOnFocus;
					};
					CityTextBox.GotFocus += clearErrorOnFocus;
					CityTextBox.Text = "Превышено время выполнения. Попробуйте VPN";
					return null;
				}
				catch (Exception ex)
				{
					CityTextBox.Text = ex.Message;
					CityTextBox.IsReadOnly = true;
					CityTextBox.FontSize = 12;
					await Task.Delay(15000);
					System.Windows.Application.Current.Shutdown();
					return null;
				}
			}
			if (!resp.IsSuccessStatusCode)
			{
				var wrongName = CityTextBox.Text;
				RoutedEventHandler clearErrorOnFocus = null;
				clearErrorOnFocus = (s, ev) =>
				{
					CityTextBox.Text = wrongName;
					CityTextBox.GotFocus -= clearErrorOnFocus;
				};
				CityTextBox.GotFocus += clearErrorOnFocus;
				CityTextBox.Text = resp.ReasonPhrase;
				return null;
			}

			var str = await resp.Content.ReadAsStringAsync();
			return ParseWeatherString(str);
		}

		/// <summary>
		/// Десериализовать данные по городу
		/// </summary>
		private Weather ParseWeatherString(string json)
		{
			var jWheather = JObject.Parse(json);

			var weather = new Weather()
			{
				DateTime = DateTime.Now,
				City = jWheather["name"]?.Value<string>(),
				CityId = jWheather["id"]?.Value<int>() ?? 0,
				Temperature = (jWheather["main"]["temp"]?.Value<float>() ?? 0) - 273.15f,
				Country = jWheather.GetValue("sys")?.Value<string>("country"),
				Weath = jWheather["weather"][0]?.Value<string>("main"),
				Description = jWheather["weather"]?[0]?["description"]?.Value<string>()
			};
			return weather.CityId == 0 ? null : weather;
		}

		/// <summary>
		/// Выбрать город по двойному нажатию
		/// </summary>
		private void citiesGrid_DoubleClicked(object sender, MouseButtonEventArgs e)
		{
			citiesGrid.Visibility = Visibility.Hidden;
			InfoPanel.Visibility = Visibility.Visible;
			var selWeather = ((DataGrid)sender).SelectedItem as Weather;
			CityTextBox.Text = selWeather.City;

			var properties = typeof(Weather).GetProperties();
			var propNames = properties.Select(x => x.Name);

			foreach (var child in  infoGrid.Children)
			{
				var lbl = child as Label;
				if (lbl != null)
				{
					var FindingName = lbl.Name.Replace("LabelInfo_", "");
					if (propNames.Contains(FindingName))
						lbl.Content = properties.Where(x => x.Name == FindingName).First().GetValue(selWeather);
				}
			}
		}

		/// <summary>
		/// Возврат к таблице
		/// </summary>
		private void back_Click(object sender, RoutedEventArgs e)
		{
			citiesGrid.Visibility = Visibility.Visible;
			InfoPanel.Visibility = Visibility.Hidden;
			UpdateGrid();
		}

		/// <summary>
		/// Обновить данные по городу
		/// </summary>
		private async void refresh_Click(object sender, RoutedEventArgs e)
		{
			delete.IsEnabled = false;
			refresh.IsEnabled = false;
			var selWeather = citiesGrid.SelectedItem as Weather;
			CityTextBox.Text = selWeather.City;
			var freshw = await GetCityWeatherDataAsync();
			selWeather.UpdateData(freshw);

			database.SaveChanges();
			delete.IsEnabled = true;
			refresh.IsEnabled = true;
			citiesGrid_DoubleClicked(citiesGrid, null);
		}

		/// <summary>
		/// Удалить данные по городу
		/// </summary>
		private void delete_Click(object sender, RoutedEventArgs e)
		{
			var selWeather = citiesGrid.SelectedItem as Weather;
			database.Weathers.Remove(selWeather);
			database.SaveChanges();
			back_Click(sender, e);
		}
	}
}
