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
            string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\optimisation_single_column.struxml";

            //string bscPathQEconcrete = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\QEconcrete.bsc";
            //string bscPathQEreinforcement = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\QEreinforcement.bsc";
            string outFolder = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputs";
            string tempPath = outFolder + "temp.struxml";
            List<string> bscPaths = new List<string>();
            //bscPaths.Add(bscPathQEconcrete);
            //bscPaths.Add(bscPathQEreinforcement);
            string bscPath = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\quantities_test.bsc";
            Model model = Model.DeserializeFromFilePath(path);

            Model modelConcrete = Model.DeserializeFromFilePath(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\single_column_concrete.struxml");
            Model modelSteel = Model.DeserializeFromFilePath(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\single_column_steel.struxml");
            Model modelTimber = Model.DeserializeFromFilePath(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\single_column_timber.struxml");
            Model modelGlulam = Model.DeserializeFromFilePath(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Single_column\single_column_glulam.struxml");


            var resultTypes = new List<Type>
            {
                typeof(QuantityEstimationConcrete),
                typeof(QuantityEstimationReinforcement),
                typeof(QuantityEstimationSteel),
                typeof(QuantityEstimationTimber),
                typeof(NodalDisplacement),
            };

            // Creating the bsc paths in C:\femdesign-api\quantities_test\scripts
            List<string> bscPathsFromResultTypes = Calculate.Bsc.BscPathFromResultTypes(resultTypes, bscPath);

            //CO2 udledning pr kg armeringsstål
            double reinforcementCarbon = 0.6841;
            double steelCarbon = 1.125 + 0.00184;
            double constructionWoodCarbon = (-680) + 728;
            double glulamCarbon = (-610) + 743;
            // CO2 udledning pr m3 beton med en massefylde på 2246 kg/m3
            Dictionary<string, double> materialCarbon = new Dictionary<string, double>();
            materialCarbon.Add("C20/25", 227.07);
            materialCarbon.Add("C25/30", 252.7);
            materialCarbon.Add("C30/37", 293.69);
            materialCarbon.Add("C35/45", 311.47);
            materialCarbon.Add("C40/50", 440.77);
            materialCarbon.Add("S 355", steelCarbon);
            materialCarbon.Add("C20", constructionWoodCarbon);
            materialCarbon.Add("GL 24c", glulamCarbon);



            //Instantiating
            double concreteVolume = 0;
            double reinforcementWeight = 0;
            double steelWeight = 0;
            double timberVolume = 0;
            double utilisation = 0;
            string utilisationSC = "";
            string chosenSection = "";


            //Read slab (hard coded in this example, probably better to look for a slab with a certain name, eg. P.1)
            //Shells.Slab slab = model.Entities.Slabs[0];


            //Sets up what type of analysis should be done
            #region Analysis Setup

            //// Setup for calculation of load combinations
            //bool NLE = true; // Non-linear elastic calculation
            //bool PL = false; // Plastic analysis
            //bool NLS = false; // Non-linear soil
            //bool Cr = false; // Cracked-section analysis
            //bool _2nd = false; // Second order analysis

            //// Skapar inställningar för analysen
            //var combItem = new Calculate.CombItem(0, 0, NLE, PL, NLS, Cr, _2nd);

            //int numLoadCombs = model.Entities.Loads.LoadCombinations.Count;
            //var combItems = new List<Calculate.CombItem>();
            //for (int i = 0; i < numLoadCombs; i++)
            //{
            //    combItems.Add(combItem);
            //}

            //// Behövs för datastrukturens skull
            //Calculate.Comb comb = new Calculate.Comb();
            //comb.CombItem = combItems.ToList();

            #endregion


            //Loop variables
            //double low = 0.17;
            //double high = 0.31;

            //Second Loop


            List<double> concreteVolumeList = new List<double>();
            List<double> reinforcementWeightList = new List<double>();
            List<double> utilisationList = new List<double>();

            List<double> GWP = new List<double>();

            foreach (KeyValuePair<string, double> entry in materialCarbon)
            {

                string materialInput = entry.Key;
                //var materialsDatabase = Materials.MaterialDatabase.DeserializeStruxml("materials.struxml");
                //var sectionsDatabase = Sections.SectionDatabase.DeserializeStruxml("sections.struxml");

                //Materials.Material newMaterial = materialsDatabase.MaterialByName(materialInput);

                //Bars.Bar excolumn = model.Entities.Bars[0];

                //string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";

                //Sections.Section currentSection = null;
                if (materialInput == "C20/25" || materialInput == "C25/30" || materialInput == "C30/37" || materialInput == "C35/45" || materialInput == "C40/50")
                {

                    
                    var materialsDatabase = Materials.MaterialDatabase.DeserializeStruxml("materials.struxml");
                    //Materials.Material newMaterial = materialsDatabase.MaterialByName(materialInput);
                    Materials.Material newMaterial = materialsDatabase.MaterialByName(materialInput);
                    modelConcrete.Materials.Material[0] = newMaterial;
                    modelConcrete.Entities.Bars[0].BarPart.ComplexMaterialObj = newMaterial;

                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    modelConcrete.SerializeModel(outPathIndividual);

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("rc", outPathIndividual, analysis, design, bscPathsFromResultTypes, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);


                    concreteVolume = ConcreteVolume();
                    concreteVolumeList.Add(concreteVolume);
                    reinforcementWeight = ReinforcementWeight();
                    reinforcementWeightList.Add(reinforcementWeight);

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationConcrete.csv");
                }
                else if (materialInput == "S 355")
                {
                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    modelSteel.SerializeModel(outPathIndividual);

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("steel", outPathIndividual, analysis, design, bscPathsFromResultTypes, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);


                    steelWeight = SteelWeight();

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationSteel.csv");

                }
                else if (materialInput == "C20")
                {
                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    modelTimber.SerializeModel(outPathIndividual);

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("timber", outPathIndividual, analysis, design, bscPathsFromResultTypes, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);


                    timberVolume = TimberVolume();

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationTimber.csv");

                }
                else if (materialInput == "GL 24c")
                {
                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    modelGlulam.SerializeModel(outPathIndividual);

                    Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, false, false, false, false, false, false, false, false, true, false, false, false, false);
                    Calculate.Design design = new Calculate.Design(autoDesign: true, check: true, applyChanges: true);
                    Calculate.FdScript fdScript = Calculate.FdScript.Design("timber", outPathIndividual, analysis, design, bscPathsFromResultTypes, "", true);
                    Calculate.Application app = new Calculate.Application();
                    app.RunFdScript(fdScript, false, true, true);


                    timberVolume = TimberVolume();

                    chosenSection = ChosenSection(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\QuantityEstimationTimber.csv");

                }


                //model.Materials.Material[0] = newMaterial;
                //model.Sections.Section[0] { currentSection};



                //string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";

                //model.SerializeModel(outPathIndividual);

                //RunAnalysis(outPathIndividual, bscPathsFromResultTypes);
                //string chosenSection = model.Sections.Section[1].Name;
                //concreteVolume = ConcreteVolume();
                //concreteVolumeList.Add(concreteVolume);
                //reinforcementWeight = ReinforcementWeight();
                //reinforcementWeightList.Add(reinforcementWeight);

                utilisation = Utilisation();
                utilisationList.Add(utilisation);

                //utilisationSC = UtilisationSC();

                double Carbon = entry.Value;

                double reinforcementRatio = (reinforcementWeight / 7850) / concreteVolume;

                //Calculate GWP, write to console app and write to list
                double totalGWP = 0;

                if (materialInput == "C20/25" || materialInput == "C25/30" || materialInput == "C30/37" || materialInput == "C35/45" || materialInput == "C40/50")
                {
                    totalGWP = Carbon * concreteVolume + reinforcementCarbon * reinforcementWeight;
                }
                else if (materialInput == "S 355")
                {
                    totalGWP = Carbon * steelWeight;
                }
                else if (materialInput == "C20")
                {
                    totalGWP = Carbon * timberVolume;
                }
                else if (materialInput == "GL 24c")
                {
                    totalGWP = Carbon * timberVolume;
                }




                Console.WriteLine(string.Format("{0} {1} {2} {3} {4} ", "GWP: ", totalGWP, materialInput, chosenSection, utilisation));
                GWP.Add(totalGWP);

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
                    if (values[0] == "TOTAL" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "reinforcement", values[5]));
                        steelWeight = ConvertTtokg(Double.Parse(values[7].Replace('.', '.'), CultureInfo.InvariantCulture));
                    }
                    counter++;
                }
            }
            return steelWeight;
        }

        public static double Utilisation()
        {
            //Read results from csv file
            double utilisation = 0;
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\NodalDisplacementsLoadCombination.csv"))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "C.1.1" &  line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));
                        utilisation = double.Parse(values[4], CultureInfo.InvariantCulture);
                        break;
                    }
                    counter++;
                }
            }

            return utilisation;

        }

        public static string UtilisationSC()
        {
            //Read results from csv file

            string utilisationSC = "";
            int counter = 0;
            using (var reader = new StreamReader(@"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputssample_slab_out\results\RCDesignShellUtilizationLoadCombination.csv"))
            {

                //Console.WriteLine("");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split("\t");
                    if (values[0] == "P.1.1" & line != "")
                    {
                        //Console.WriteLine(string.Format("{0} {1} {2}", values[0], "concrete", values[10]));

                        utilisationSC = values[7];
                    }
                    counter++;
                }
            }

            return utilisationSC;
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