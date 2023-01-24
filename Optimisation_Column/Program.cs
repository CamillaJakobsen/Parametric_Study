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
            //string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\sample_optimisation_slab.struxml";
            //string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\optimisation_single_column.struxml";

            //string bscPathQEconcrete = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\QEconcrete.bsc";
            string bscPathConcreteColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_C20_utilisation.bsc";
            string bscPathSteelColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_steel_utilisation.bsc";
            string bscPathTimberColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_timber_utilisation.bsc";
            string bscPathGlulamColumnUtilisation = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\Column_glulam_utilisation.bsc";
            string outFolder = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputs";
            string tempPath = outFolder + "temp.struxml";

            string bscPath = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\quantities_test.bsc";

            Model modelConcrete = Model.DeserializeFromFilePath("single_column_concrete_C20.struxml");
            Model modelSteel = Model.DeserializeFromFilePath("single_column_steel.struxml");
            Model modelTimber = Model.DeserializeFromFilePath("single_column_timber.struxml");
            Model modelGlulam = Model.DeserializeFromFilePath("single_column_glulam.struxml");


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
            
            //CO2 udledning pr kg armeringsstål
            double reinforcementCarbon = 0.6841;
            double steelCarbon = 1.125 + 0.00184;
            double constructionWoodCarbon = (-680) + 728;
            double glulamCarbon = (-610) + 743;

            // CO2 udledning pr m3 beton med en massefylde på 2246 kg/m3
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
            double utilisation = 0;
            string chosenSection = "";

            foreach (KeyValuePair<string, double> entry in materialCarbon)
            {
                string material = "";
                string materialInput = entry.Key;

                if (materialInput == "C20/25")
                {
                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    modelConcrete.SerializeModel(outPathIndividual);
                    

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("rc", outPathIndividual, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);


                    concreteVolume = ConcreteVolume();
                    reinforcementWeight = ReinforcementWeight();

                    //RunAnalysis(outPathIndividual, bscPathUtilisation);

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationConcrete.csv");
                    utilisation = Utilisation(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\Column_C20_utilisation.csv");
                    material = materialInput;
                }
                else if (materialInput == "steel")
                {
                    string outPathIndividual2 = outFolder + "sample_slab_out" + ".struxml";
                    modelSteel.SerializeModel(outPathIndividual2);

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, true, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("steel", outPathIndividual2, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);

                   
                    steelWeight = SteelWeight();

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationSteel.csv");
                    utilisation = Utilisation(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\Column_steel_utilisation.csv");
                    material = modelSteel.Materials.Material[0].ToString();
                }
                else if (materialInput == "timber")
                {
                    string outPathIndividual3 = outFolder + "sample_slab_out" + ".struxml";
                    modelTimber.SerializeModel(outPathIndividual3);

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, true, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("timber", outPathIndividual3, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);


                    timberVolume = TimberVolume();

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationTimber.csv");
                    utilisation = Utilisation(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\Column_timber_utilisation.csv");
                    material = modelTimber.Materials.Material[0].ToString();
                }
                else if (materialInput == "glulam")
                {
                    string outPathIndividual4 = outFolder + "sample_slab_out" + ".struxml";
                    modelGlulam.SerializeModel(outPathIndividual4);

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, true, false, false, true, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("timber", outPathIndividual4, analysis, design, bscPaths, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);


                    timberVolume = TimberVolume();

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationTimber.csv");
                    utilisation = Utilisation(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\Column_glulam_utilisation.csv");
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

                Console.WriteLine(string.Format("{0} {1} {2} {3} {4} ", "GWP: ", totalGWP, material, chosenSection, utilisation));

            }

        }

        public static void RunAnalysis(string modelPath, List<string> bscFilePaths)
        {
            Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
            Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
            Calculate.FdScript fdScript = Calculate.FdScript.Design("rc", modelPath, analysis, design, bscFilePaths, "", true);
            Calculate.Application app = new Calculate.Application();
            app.RunFdScript(fdScript, false, true, true);

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