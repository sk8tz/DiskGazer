﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

using DiskGazer.Models.Win32;

namespace DiskGazer.Models
{
	internal static class DiskChecker
	{
		/// <summary>
		/// Get disk information by P/Invoke.
		/// </summary>
		/// <param name="physicalDrive">Index number of physical drive</param>
		/// <returns>Disk information</returns>
		internal static DiskInfo GetDiskInfo(int physicalDrive)
		{
			var info = new DiskInfo { PhysicalDrive = physicalDrive };

			SafeFileHandle hFile = null;

			try
			{
				hFile = NativeMethod.CreateFile(
					String.Format(@"\\.\PhysicalDrive{0}", physicalDrive),
					NativeMethod.GENERIC_READ | NativeMethod.GENERIC_WRITE, // Administrative privilege is required. GENERIC_WRITE is for IOCTL_ATA_PASS_THROUGH.
					NativeMethod.FILE_SHARE_READ | NativeMethod.FILE_SHARE_WRITE,
					IntPtr.Zero,
					NativeMethod.OPEN_EXISTING,
					NativeMethod.FILE_ATTRIBUTE_NORMAL,
					IntPtr.Zero);
				if (hFile == null || hFile.IsInvalid)
				{
					// This is normal when this application is not run by administrator.
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get handle to disk.");
				}

				// ---------------------------------
				// Use IOCTL_STORAGE_QUERY_PROPERTY.
				// ---------------------------------
				var storageQuery = new NativeMethod.STORAGE_PROPERTY_QUERY();
				storageQuery.PropertyId = NativeMethod.StorageDeviceProperty;
				storageQuery.QueryType = NativeMethod.PropertyStandardQuery;

				NativeMethod.STORAGE_DEVICE_DESCRIPTOR storageDescriptor;
				uint bytesReturned1;

				var result1 = NativeMethod.DeviceIoControl(
					hFile,
					NativeMethod.IOCTL_STORAGE_QUERY_PROPERTY,
					ref storageQuery,
					(uint)Marshal.SizeOf(typeof(NativeMethod.STORAGE_PROPERTY_QUERY)),
					out storageDescriptor,
					(uint)Marshal.SizeOf(typeof(NativeMethod.STORAGE_DEVICE_DESCRIPTOR)),
					out bytesReturned1,
					IntPtr.Zero);
				if (result1 == false)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get disk information.");

				// Convert to byte array.
				var size = Marshal.SizeOf(storageDescriptor);
				var ptr = Marshal.AllocHGlobal(size);
				var bytes = new byte[size];

				Marshal.StructureToPtr(storageDescriptor, ptr, true);
				Marshal.Copy(ptr, bytes, 0, size);
				Marshal.FreeHGlobal(ptr);

				// Set values.
				info.Vendor = ConvertBytesToString(bytes, (int)storageDescriptor.VendorIdOffset).Trim();
				info.Product = ConvertBytesToString(bytes, (int)storageDescriptor.ProductIdOffset).Trim();
				info.IsRemovable = storageDescriptor.RemovableMedia;
				info.BusType = ConvertBusTypeToString(storageDescriptor.BusType);

				// -------------------------------
				// Use IOCTL_DISK_GET_LENGTH_INFO.
				// -------------------------------
				long diskSize;
				uint bytesReturned2;

				var result2 = NativeMethod.DeviceIoControl(
					hFile,
					NativeMethod.IOCTL_DISK_GET_LENGTH_INFO,
					IntPtr.Zero,
					0,
					out diskSize,
					(uint)Marshal.SizeOf(typeof(long)),
					out bytesReturned2,
					IntPtr.Zero);
				if (result2 == false)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get disk size.");

				// Set value.
				info.SizePInvoke = diskSize;

				// ---------------------------
				// Use IOCTL_ATA_PASS_THROUGH.
				// ---------------------------
				var ataQuery = new NativeMethod.ATAIdentifyDeviceQuery();
				ataQuery.data = new ushort[256];

				ataQuery.header.Length = (ushort)Marshal.SizeOf(typeof(NativeMethod.ATA_PASS_THROUGH_EX));
				ataQuery.header.AtaFlags = NativeMethod.ATA_FLAGS.ATA_FLAGS_DATA_IN;
				ataQuery.header.DataTransferLength = (uint)ataQuery.data.Length * 2; // Size of "data" in bytes
				ataQuery.header.TimeOutValue = 3; // Sec
				ataQuery.header.DataBufferOffset = Marshal.OffsetOf(typeof(NativeMethod.ATAIdentifyDeviceQuery), "data");
				ataQuery.header.PreviousTaskFile = new byte[8];
				ataQuery.header.CurrentTaskFile = new byte[8];
				ataQuery.header.CurrentTaskFile[6] = 0xec; // ATA IDENTIFY DEVICE

				uint bytesReturned3;

				var result3 = NativeMethod.DeviceIoControl(
					hFile,
					NativeMethod.IOCTL_ATA_PASS_THROUGH,
					ref ataQuery,
					(uint)Marshal.SizeOf(typeof(NativeMethod.ATAIdentifyDeviceQuery)),
					ref ataQuery,
					(uint)Marshal.SizeOf(typeof(NativeMethod.ATAIdentifyDeviceQuery)),
					out bytesReturned3,
					IntPtr.Zero);
				if (result3)
				{
					const int index = 217; // Word index of nominal media rotation rate (1 means non-rotating media.)
					info.NominalMediaRotationRate = ataQuery.data[index];
				}
				else
				{
					// None. It is normal that IOCTL_ATA_PASS_THROUGH fails when used to external or removable media.
				}
			}
			catch (Win32Exception ex)
			{
				Debug.WriteLine("{0} (Code: {1}).", ex.Message.Substring(0, ex.Message.Length - 1), ex.ErrorCode);
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				throw;
			}
			finally
			{
				if (hFile != null)
				{
					// CloseHandle is inappropriate to close SafeFileHandle.
					// Dispose method is not necessary because Close method will call it internally.
					hFile.Close();
				}
			}

			return info;
		}

		private static string ConvertBytesToString(byte[] source, int indexStart)
		{
			if ((indexStart <= 0) || // If no data, start index is zero.
				(source.Length - 1 <= indexStart))
				return String.Empty;

			var indexEnd = Array.IndexOf(source, default(byte), indexStart); // default(byte) is null.
			if (indexEnd <= 0)
				return String.Empty;

			return Encoding.ASCII.GetString(source, indexStart, indexEnd - indexStart);
		}

		private static string ConvertBusTypeToString(NativeMethod.STORAGE_BUS_TYPE type)
		{
			switch (type)
			{
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeScsi:
					return "SCSI";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeAtapi:
					return "ATAPI";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeAta:
					return "ATA";
				case NativeMethod.STORAGE_BUS_TYPE.BusType1394:
					return "1394";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeSsa:
					return "SSA";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeFibre:
					return "Fibre";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeUsb:
					return "USB";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeRAID:
					return "RAID";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeiScsi:
					return "iSCSI";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeSas:
					return "SAS";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeSata:
					return "SATA";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeSd:
					return "SD";
				case NativeMethod.STORAGE_BUS_TYPE.BusTypeMmc:
					return "MMC";
				default:
					return String.Empty;
			}
		}
	}
}