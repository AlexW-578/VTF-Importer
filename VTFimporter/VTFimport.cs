using System;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using FrooxEngine;
using BaseX;
using HarmonyLib;
using NeosModLoader;
using System.Diagnostics;
using CodeX;
using System.Collections.Generic;

namespace VTFimporter
{
	public class VTFimport : NeosMod
	{
		public override string Name => "VTF Importer";
		public override string Author => "AlexW-578";
		public override string Version => "0.1.0";
		public override string Link => "https://github.com/AlexW-578/VTF-Importer";

		public static ModConfiguration config;

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> FILE_FORMAT = new ModConfigurationKey<string>("fileFormat", "File Format", () => "png");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> VFTCMD_PATH = new ModConfigurationKey<string>("vftCmdPath", "vftCmd Path", () => "vtfLib");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> OUT_DIR = new ModConfigurationKey<string>("outputDirectory", "Output Directory for converted files", () => "temp");
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("co.uk.AlexW-578.VTFimporter");
			harmony.PatchAll();
			config = GetConfiguration();
		}
		
		[HarmonyPatch(typeof(UniversalImporter), "Import",  new Type[] { typeof(AssetClass), typeof(IEnumerable<string>), typeof(World), typeof(float3), typeof(floatQ), typeof(bool) })]
		class VTFImporterPatch
		{
			public static bool Prefix(AssetClass assetClass, IEnumerable<string> files, World world, float3 position, floatQ rotation, bool silent = false)
			{
				
				List<string> vtfFiles = new List<string>();
				List<string> notVtfFiles = new List<string>();
				foreach (string file in files)
				{
					Uri uri = new Uri(file);
					bool validVTF = false;
					if (uri.Scheme == "file" && string.Equals(Path.GetExtension(file), ".vtf",
					StringComparison.OrdinalIgnoreCase))
					{
						validVTF = true;
					}
					if (!validVTF)
					{
						notVtfFiles.Add(file);
					}
					else
					{
						string format = config.GetValue(FILE_FORMAT);
						string vtfcmdPath = $"{Path.GetFullPath(config.GetValue(VFTCMD_PATH))}\\VTFCmd.exe";
						string outputPath = Path.GetTempPath();
						outputPath = outputPath.Remove(outputPath.Length - 1);
						if (config.GetValue(OUT_DIR) != "temp")
						{
							outputPath = config.GetValue(OUT_DIR);
						}
												
						// VTFcmd Args
						string[] Args = {
						$"-file \"{file}\"",
						$"-output \"{outputPath}\"",
						$"-exportformat \"{format}\"",
						};

						string ArgsString = String.Join(" ", Args);
						Process.Start(vtfcmdPath, ArgsString).WaitForExit();
                        
						string outputFile = $"{outputPath}\\{Path.GetFileNameWithoutExtension(file)}.{format}";
						vtfFiles.Add(outputFile);
					}
				}
				if(vtfFiles.Count != 0)
                {
					UniversalImporter.Import(AssetClass.Texture, vtfFiles, world, position, rotation, silent);
					if (notVtfFiles.Count != 0)
					{
						UniversalImporter.Import(assetClass, notVtfFiles, world, position, rotation, silent);
					}
					return false;
				}
				
				return true;
			}

		}
		


	}
	
}
