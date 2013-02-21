/*
 * This is a really ugly and simple file parser, but it does it's job so yay :P.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GenerateAutoComplete {
	class Program {
		static void Main(string[] args) {
			if (args.Length < 2) {
				Console.WriteLine("Command line param required for the source file's name/location (1) and output file's name/location (2).");
				Console.ReadKey();
				return;
			}
			try {
				List<string> Classes = new List<string>();
				List<string> Structs = new List<string>();
				Dictionary<string, List<string>> Enums = new Dictionary<string, List<string>>();
				Dictionary<string, List<string[]>> Stuff = new Dictionary<string, List<string[]>>();
				int Depth = 0;
				string LatestStruct = "";
				string LatestEnum = "";
				using (FileStream fs = new FileStream(args[0], FileMode.Open)) {
				using (StreamReader fsr = new StreamReader(fs)) {
					using (FileStream output = new FileStream(args[1], FileMode.Create)) {
					using (StreamWriter outputw = new StreamWriter(output)) {
						while (fsr.Peek() != -1) {
							string line = fsr.ReadLine().Replace("\t", "").Replace(";", "");
							if (line.Contains("/*!")) {
								fsr.ReadLine();
								fsr.ReadLine();
								continue;
							}
							bool isConst = line.Contains("const");
							if (isConst) line = line.Replace("const", "");
							string[] bits = line.Split(new char[] { ' ', '{', '(', ':', ',' }, 3);
							if (bits.Length < 2 && Depth != 2 || line.Contains('}') && !line.Contains("class")) {
								if (line.Contains('}')) {
									Depth--;
									if (Depth < 0) Depth = 0;
									Console.WriteLine("Depth down: " + Depth);
								}
								continue;
							}
							switch (bits[0]) {
								case "class":
									Classes.Add(bits[1]);
									Console.WriteLine("Class: " + bits[1]);
									break;
								case "struct":
									Console.WriteLine("Struct: " + bits[1]);
									Structs.Add(bits[1]);
									Stuff.Add(bits[1], new List<string[]>());
									LatestStruct = bits[1];
									Depth++;
									break;
								case "enum":
									Console.WriteLine("Enum: " + bits[1]);
									Enums.Add(LatestStruct + "::" + bits[1], new List<string>());
									LatestEnum = LatestStruct + "::" + bits[1];
									Depth++;
									break;
								default:
									if (Depth == 1) { // Add to Struct
										if (bits.Length >= 3 && bits[2].Contains(')')) {
											Stuff[LatestStruct].Add(new string[] { bits[0], bits[1], bits[2].Split(new char[] { ')' })[0] });
											Console.WriteLine("Add to latest struct: " + bits[0] + " " + bits[1]);
										} else {
											Stuff[LatestStruct].Add(new string[] { bits[0], bits[1], (isConst ? "const" : "") });
											Console.WriteLine("Add to latest struct: " + bits[0] + " " + bits[1]);
										}
									} else if (Depth == 2) { // Add to Enum
										Enums[LatestEnum].Add(bits[0]);
										Console.WriteLine("Add to latest enum: " + bits[0]);
									}
									//System.Threading.Thread.Sleep(1000);
									break;
							}
						}
						// Done parsing
						// Starting to write now
						outputw.WriteLine("{");
						outputw.WriteLine("\"scope\": \"source.ms\",");
						outputw.WriteLine("\"completions\": [");
						foreach (string class_ in Classes)
							outputw.WriteLine("{\"trigger\": \"" + class_ + "\",\"contents\": \"" + class_ + "\" },");
						outputw.WriteLine();
						foreach (string struct_ in Structs)
							outputw.WriteLine("{\"trigger\": \"" + struct_ + "\",\"contents\": \"" + struct_ + "\" },");
						outputw.WriteLine();
						foreach (KeyValuePair<string, List<string>> kvp in Enums)
							foreach (string enumbit in kvp.Value)
								outputw.WriteLine("{\"trigger\": \"" + kvp.Key + "::" + enumbit + "\",\"contents\": \"" + kvp.Key + "::" + enumbit + "\" },");
						outputw.WriteLine();
						foreach (KeyValuePair<string, List<string[]>> kvp in Stuff)
							foreach (string[] params_ in kvp.Value) {
								if (params_[0] == "Void") {
									Console.WriteLine("VoidParam: "+params_[2]);
									outputw.Write("{\"trigger\": \"Void " + kvp.Key + "." + params_[1] + "(" + params_[2] + ")\",\"contents\": \"" + params_[1] + "(");
									int i = 1;
									string[] mess = params_[2].Split(',');
									foreach (string p in mess) {
										outputw.Write("${"+i+":"+p+"}");
										if (i != mess.Length)
											outputw.Write(", ");
										i++;
									}
									outputw.WriteLine(")\" },");
								} else {
									Console.WriteLine("OtherParam: " + params_[0]);
									outputw.WriteLine("{\"trigger\": \"" + params_[0] + " " + kvp.Key + "." + params_[1] + "\",\"contents\": \"" + params_[1] + "\" },");
								}
							}
						outputw.WriteLine("]");
						outputw.WriteLine("}");
					}
					}
				}
				}
				Console.WriteLine("Done!");
				Console.WriteLine("Printing maybe useful other data...");
				Console.WriteLine("CLASSES");
				foreach (string a in Classes)
					Console.Write(a+"|");
				Console.WriteLine();
				Console.WriteLine("STRUCTS");
				foreach (string a in Structs)
					Console.Write(a + "|");
				Console.WriteLine();
				Console.WriteLine("ENUM NAMES");
				foreach (KeyValuePair<string, List<string>> kvp in Enums)
					Console.Write(kvp.Key.Split(new string[]{"::"}, StringSplitOptions.None)[1]+"|");
				Console.WriteLine();

				Console.ReadKey();
				return;
			} catch (Exception e) {
				Console.WriteLine("Error: "+e.Message);
				Console.ReadKey();
				return;
			}
		}
	}
}
