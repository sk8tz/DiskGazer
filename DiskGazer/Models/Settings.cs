﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using DiskGazer.Common;

namespace DiskGazer.Models
{
	public class Settings : NotificationObject
	{
		private Settings()
		{ }

		public static Settings Current
		{
			get { return _current ?? (_current = new Settings()); }
		}
		private static Settings _current;


		#region Settings

		/// <summary>
		/// Index number of physical drive
		/// </summary>
		public int PhysicalDrive
		{
			get { return _physicalDrive; }
			set
			{
				_physicalDrive = value;
				RaisePropertyChanged();
			}
		}
		private int _physicalDrive = 0;

		/// <summary>
		/// Block size (KiB)
		/// </summary>
		public int BlockSize
		{
			get { return _blockSize; }
			set
			{
				_blockSize = value;
				RaisePropertyChanged();
			}
		}
		private int _blockSize = 1024;

		/// <summary>
		/// Block offset (KiB)
		/// </summary>
		/// <remarks>To divide block size so as to produce block offset.</remarks>
		public int BlockOffset
		{
			get { return _blockOffset; }
			set
			{
				_blockOffset = value;
				RaisePropertyChanged();
			}
		}
		private int _blockOffset = 0;

		/// <summary>
		/// Area size (MiB)
		/// </summary>
		public int AreaSize
		{
			get { return _areaSize; }
			set
			{
				_areaSize = value;
				RaisePropertyChanged();
			}
		}
		private int _areaSize = 1024;

		/// <summary>
		/// Area location (MiB)
		/// </summary>
		public int AreaLocation
		{
			get { return _areaLocation; }
			set
			{
				_areaLocation = value;
				RaisePropertyChanged();
			}
		}
		private int _areaLocation = 0;

		/// <summary>
		/// Number of runs
		/// </summary>
		public int NumRun
		{
			get { return _numRun; }
			set
			{
				_numRun = value;
				RaisePropertyChanged();
			}
		}
		private int _numRun = 5;

		/// <summary>
		/// Method for reading
		/// </summary>
		public ReadMethod Method
		{
			get { return _method; }
			set
			{
				_method = value;
				RaisePropertyChanged();
			}
		}
		private ReadMethod _method = ReadMethod.Native;

		/// <summary>
		/// Whether to remove outliers
		/// </summary>
		public bool WillRemoveOutlier
		{
			get { return _willRemoveOutlier; }
			set
			{
				_willRemoveOutlier = value;
				RaisePropertyChanged();
			}
		}
		private bool _willRemoveOutlier = true;

		/// <summary>
		/// Whether to save screenshot and log automatically.
		/// </summary>
		public bool WillSaveScreenshotLog
		{
			get { return _willSaveScreenshotLog; }
			set
			{
				_willSaveScreenshotLog = value;
				RaisePropertyChanged();
			}
		}
		private bool _willSaveScreenshotLog = false;

		#endregion
	}
}