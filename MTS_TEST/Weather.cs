using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MTS_TEST
{
	public class Weather : INotifyPropertyChanged
	{
		DateTime dateTime;
		float temperature;
		string weath;
		string description;
		public int Id { get; set; }
		public int CityId { get; set; }
		public string City { get; set; }
		public string Country { get; set; }
		public DateTime DateTime { 
			get
			{
				return dateTime;
			}
			set
			{
				dateTime = value;
				OnPropertyChanged("DateTime");
			} 
		}
		public float Temperature { get
			{ 
				return temperature;
			} set
			{
				temperature = value;
				OnPropertyChanged("Temperature");
			}
		}
		public string Weath { get
			{
				return weath;
			}
			set
			{
				weath = value;
				OnPropertyChanged("Weath");
			}
		}
		public string Description { get
			{
				return description;
			}
			set
			{
				description = value;
				OnPropertyChanged("Description");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName]string prop = "")
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
		}

		public void UpdateData(Weather fresh)
		{
			if (fresh != null)
			{
				this.DateTime = fresh.DateTime;
				this.Weath = fresh.Weath;
				this.Temperature = fresh.Temperature;
				this.Description = fresh.Description;
			}
		}
	}
}
