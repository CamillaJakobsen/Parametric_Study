using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FemDesign;
using System.Globalization;
//using FemDesignProgram.Helpers;
using System.ComponentModel;
using FemDesign.Materials;
using FemDesign.Results;
using System.Reflection;
using FemDesign.Bars;


namespace FemDesign.Examples
{
    public class Optimisation_Column
    {

        public static void Main(string[] args)
        {
            //bscpaths";
            string bscPathConcreteColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_C20_utilisation.bsc";
            string bscPathSteelColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_steel_utilisation.bsc";
            string bscPathTimberColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_timber_utilisation.bsc";
            string bscPathGlulamColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_glulam_utilisation.bsc";
            string outFolder = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputs";
            string tempPath = outFolder + "temp.struxml";

            // bsc paths
            string bscPath = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\quantities_test.bsc";
            // Modelpaths
            string pathConcrete = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\femdesign-api-master\Optimisation_Column\single_column_concrete_C20.struxml";
            string pathSteel = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\femdesign-api-master\Optimisation_Column\single_column_steel.struxml";
            string pathTimber = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\femdesign-api-master\Optimisation_Column\single_column_timber.struxml";
            string pathGlulam = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\femdesign-api-master\Optimisation_Column\single_column_glulam.struxml";

            //deserialize models
            Model modelConcrete = Model.DeserializeFromFilePath(pathConcrete);
            Model modelSteel = Model.DeserializeFromFilePath(pathSteel);
            Model modelTimber = Model.DeserializeFromFilePath(pathTimber);
            Model modelGlulam = Model.DeserializeFromFilePath(pathGlulam);

            // bsc path types
            var resultTypes = new List<Type>
            {
                typeof(QuantityEstimationConcrete),
                typeof(QuantityEstimationReinforcement),
                typeof(QuantityEstimationSteel),
                typeof(QuantityEstimationTimber),
            };

            // Creating the bsc paths in C:\femdesign-api\quantities_test\scripts
            List<string> bscPaths = Calculate.Bsc.BscPathFromResultTypes(resultTypes, bscPath);
            bscPaths.Add(bscPathConcreteColumnUtilisation);
            bscPaths.Add(bscPathSteelColumnUtilisation);
            bscPaths.Add(bscPathTimberColumnUtilisation);
            bscPaths.Add(bscPathGlulamColumnUtilisation);
            
            //CO2 emissions per kg reinforcement steel
            double reinforcementCarbon = 0.6841;
            //CO2 emissions per kg steel
            double steelCarbon = 1.125 + 0.00184;
            //CO2 emissions per m3 timber
            double constructionWoodCarbon = (-680) + 728;
            //CO2 emissions per m3 glulam
            double glulamCarbon = (-610) + 743;

            // CO2 emissions per m3 concrete with a density of 2246 kg/m3 and create dictionary with the various GWP values
            Dictionary<string, double> materialCarbon = new Dictionary<string, double>();
            materialCarbon.Add("C20/25", 227.07);
            materialCarbon.Add("steel", steelCarbon);
            materialCarbon.Add("timber", constructionWoodCarbon);
            materialCarbon.Add("glulam", glulamCarbon);


            //Instantiating
            double concreteVolume = 0;
            double reinforcementWeight = 0;
            double steelWeight = 0;
            double timberVolume = 0;
            string chosenSection = "";
            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("TotalGWP, Material, CrossSection");

            //Loop over the various GWP values
            foreach (KeyValuePair<string, double> entry in materialCarbon)
            {
                string material = "";
                string materialInput = entry.Key;

                if (materialInput == "C20/25")
                {
                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    modelConcrete.SerializeModel(outPathIndividual);
                    
                    // Run the analysis
                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("rc", outPathIndividual, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);

                    //Calculate quantity
                    concreteVolume = ConcreteVolume();
                    reinforcementWeight = ReinforcementWeight();

                    //Assign the most optimal section
                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationConcrete.csv");
                    material = materialInput;
                }
                else if (materialInput == "steel")
                {
                    string outPathIndividual2 = outFolder + "sample_slab_out" + ".struxml";
                    modelSteel.SerializeModel(outPathIndividual2);

                    // Run the analysis
                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, true, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("steel", outPathIndividual2, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);

                    //Calculate quantity
                    steelWeight = SteelWeight();

                    //Assign the most optimal section
                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationSteel.csv");
                    material = modelSteel.Materials.Material[0].ToString();
                }
                else if (materialInput == "timber")
                {
                    string outPathIndividual3 = outFolder + "sample_slab_out" + ".struxml";
                    modelTimber.SerializeModel(outPathIndividual3);

                    // Run the analysis
                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, true, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("timber", outPathIndividual3, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);

                    //Calculate quantity
                    timberVolume = TimberVolume();

                    //Assign the most optimal section
                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationTimber.csv");
                    material = modelTimber.Materials.Material[0].ToString();
                }
                else if (materialInput == "glulam")
                {
                    string outPathIndividual4 = outFolder + "sample_slab_out" + ".struxml";
                    modelGlulam.SerializeModel(outPathIndividual4);

                    // Run the analysis
                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, true, false, false, true, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("timber", outPathIndividual4, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);

                    //Calculate quantity
                    timberVolume = TimberVolume();

                    //Assign the most optimal section
                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationTimber.csv");
                    material = modelGlulam.Materials.Material[0].ToString();
                }

                double Carbon = entry.Value;

                //Calculate GWP, write to console app and write to list
                double totalGWP = 0;

                if (materialInput == "C20/25" )
                {
                    totalGWP = Carbon * concreteVolume + reinforcementCarbon * reinforcementWeight;
                }
                else if (materialInput == "steel")
                {
                    totalGWP = Carbon * steelWeight;
                }
                else if (materialInput == "timber")
                {
                    totalGWP = Carbon * timberVolume;
                }
                else if (materialInput == "glulam")
                {
                    totalGWP = Carbon * timberVolume;
                }

                Console.WriteLine(string.Format("{0} {1} {2} {3} ", "GWP: ", totalGWP, material, chosenSection));
                //Output file to csv file
                csvContent.AppendLine(string.Format("{0} {1} {2} {3}", totalGWP, material, chosenSection));
                string csvpath = "C:\\Users\\camil\\OneDrive\\OneDrive_Privat\\OneDrive\\femdesign-api-master\\Optimisation\\csvOutput.txt";
                File.AppendAllText(csvpath, csvContent.ToString());
            }

        }

        public static double ConcreteVolume()
        {
            //Read results from csv file
            double concreteVolume = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationConcrete.csv"))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "TOTAL" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));
                        concreteVolume = double.Parse(values[8], CultureInfo.InvariantCulture);
                    }
                    counter++;
                }
            }

            return concreteVolume;
        }
        public static double TimberVolume()
        {
            //Read results from csv file
            double timberVolume = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationTimber.csv"))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "-" & line != "")
                    {
                        string lengthString = values[6];
                        double length = Double.Parse(lengthString.Replace('.', '.'), CultureInfo.InvariantCulture);
                        string volumeString = values[4];
                        string[] volumeStringSplitted = volumeString.Split(' ', 'x');
                        timberVolume = ConvertmmTom(double.Parse(volumeStringSplitted[1], CultureInfo.InvariantCulture)) * ConvertmmTom(double.Parse(volumeStringSplitted[2], CultureInfo.InvariantCulture)) * length;
                    }
                    counter++;
                }
            }

            return timberVolume;
        }
        public static double ReinforcementWeight()
        {
            //Read results from csv file
            double reinforcementWeight = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationReinforcement.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "TOTAL" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "reinforcement", values[5]));
                        reinforcementWeight = Double.Parse(values[5].Replace('.', '.'), CultureInfo.InvariantCulture);
                    }
                    counter++;
                }
            }
            return reinforcementWeight;
        }
        
        public static string ChosenSection(string pathSection)
        {
            //Read results from csv file
            string chosenSection = "";
            int counter = 0;
            
            using (var reader = new StreamReader(pathSection))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "-" & values[0] != "" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));
                        chosenSection = values[4];
                    }
                    counter++;
                }
            }

            return chosenSection;
        }

        public static double SteelWeight()
        {
            //Read results from csv file
            double steelWeight = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationSteel.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "-" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "reinforcement", values[5]));
                        steelWeight = Double.Parse(values[5].Replace('.', '.'), CultureInfo.InvariantCulture);
                    }
                    counter++;
                }
            }
            return steelWeight;
        }

        public static double Utilisation(string utilisationPath)
        {
            //Read results from csv file
            double utilisation = 0;
            int counter = 0;
            using (var reader = new StreamReader(utilisationPath))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "C.1.1" &  line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));
                        utilisation = double.Parse(values[1], CultureInfo.InvariantCulture);
                        break;
                    }
                    counter++;
                }
            }

            return utilisation;

        }

        public static double ConvertTtokg(double t)
        {
            return t * 1000;
        }
        public static double ConvertmmTom(double mm)
        {
            return mm / 1000;
        }
    }
}