using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FemDesign;
using System.Globalization;
using Optimisation;
using System.ComponentModel;
using FemDesign.Results;
using System.Xml.Linq;

namespace FemDesign.Examples
{
    public class Optimisation
    {

        public static void Main(string[] args)
        {
            // The struxml file that is used for the optimation is inputted here
            string path = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\sample_optimisation_slab_custom.struxml";

            // The path for the output as well at the temporary outputs are inputted here
            string outFolder = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\Optimisation\Outputs";
            string tempPath = outFolder + "temp.struxml";

            // The path for the bsc files are inputted here
            string bscPath = @"C:\Users\camil\OneDrive\OneDrive_Privat\OneDrive\Bygningsdesign kandidat\Speciale\femdesign-api\quantities_test.bsc";

            // The model is being deserialised from the path
            Model model = Model.DeserializeFromFilePath(path);

            // The needed results outputs are listed here
            var resultTypes = new List<Type>
            {
                typeof(QuantityEstimationConcrete),
                typeof(QuantityEstimationReinforcement),
                typeof(RCShellUtilization),
            };

            // The bsc paths are created and placed in the bscPath location
            List<string> bscPathsFromResultTypes = Calculate.Bsc.BscPathFromResultTypes(resultTypes, bscPath);


            // CO2 emissions per m3 concrete with a density of 2246 kg/m3 (according to Table 7 (BR18))
            Dictionary<string, double> concreteStrength = new Dictionary<string, double>();
            concreteStrength.Add("C20/25", 227.07);
            concreteStrength.Add("C25/30", 252.7);
            concreteStrength.Add("C30/37", 293.69);
            concreteStrength.Add("C35/45", 311.47);
            concreteStrength.Add("C40/50", 440.77);

            //CO2 emissions per kg reinforcing steel
            double reinforcementCarbon = 0.6841;

            //Instatiating the variables;
            double concreteVolume = 0;
            double reinforcementWeight = 0;
            double utilisation = 0;
            string utilisationSC = "";

            //Loop variables for thickness, the chosen interval for the thickness is chosen here
            double low = 0.18;
            double high = 0.31;

            //Write the first line of the outputs
            Console.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6} {7}", "TotalGWP", "Thickness", "MaterialInput", "Utilisation", "UtilisationSC", "ReinforcementRatio", "ConcreteGWP", "ReinforcementGWP"));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TotalGWP" + "Thickness" + "MaterialInput" + "Utilisation" + "UtilisationSC" + "ReinforcementRatio" + "ConcreteGWP" + "ReinforcementGWP" + Environment.NewLine);

            //Loop over the different concrete strengths
            foreach (KeyValuePair<string, double> entry in concreteStrength)
            {
                //Change the material of the slab;
                var materialInput = entry.Key;
                var materialsDatabase = Materials.MaterialDatabase.DeserializeStruxml("materials.struxml");
                Materials.Material newMaterial = materialsDatabase.MaterialByName(materialInput);
                model.Materials.Material[0].Concrete = newMaterial.Concrete;
                Materials.Material materialSlab = model.Entities.Slabs[0].Material;

                //Loop over the interval og thickness
                for (double i = low; i < high; i = i + 0.01)
                {
                    //Set thickness of slab
                    double thickness = i;

                    //Change the thickness of slab;
                    Shells.Slab slab = model.Entities.Slabs[0];
                    slab.SlabPart.Thickness[0].Value = Math.Round(thickness, 3);
                    //Save temporary model
                    string outPathIndividual = outFolder + "sample_slab_out" + ".struxml";
                    model.SerializeModel(outPathIndividual);

                    //Run analysis and get results
                    RunAnalysis(outPathIndividual, bscPathsFromResultTypes);
                    concreteVolume = ConcreteVolume();
                    //concreteVolumeList.Add(concreteVolume);
                    reinforcementWeight = ReinforcementWeight();
                    //reinforcementWeightList.Add(reinforcementWeight);
                    utilisation = Utilisation();
                    //utilisationList.Add(utilisation);
                    utilisationSC = UtilisationSC();

                    //stating the current value of GWP of the concrete
                    double concreteCarbon = entry.Value;
                    
                    //calculation the reinforcement ratio
                    double reinforcementRatio = (reinforcementWeight / 7850) / concreteVolume;

                    //Calculate GWP, write to console app and write to list
                    double concreteGWP = concreteCarbon * concreteVolume;
                    double reinforcementGWP = reinforcementCarbon * reinforcementWeight;
                    double totalGWP = concreteCarbon * concreteVolume + reinforcementCarbon * reinforcementWeight;
                    Console.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6} {7}", totalGWP, thickness, materialInput, utilisation, utilisationSC, reinforcementRatio, concreteGWP, reinforcementGWP));
                    //Output file to csv file
                    sb.AppendLine(totalGWP + thickness + materialInput + utilisation + utilisationSC + reinforcementRatio + concreteGWP + reinforcementGWP + Environment.NewLine);

                }

            }
            //Output file to csv file
            


        }
    
        //Set-up for running the analysis
        public static void RunAnalysis(string modelPath, List<string> bscFilePaths)
        {
           Calculate.Analysis analysis = new Calculate.Analysis(null, null, null, null, true, false, false, false, false, false, false, false, false, false, false, false, false);
           Calculate.Design design = new Calculate.Design(true, false);
           Calculate.FdScript fdScript = Calculate.FdScript.Design("rc", modelPath, analysis, design, bscFilePaths, "", true);
           Calculate.Application app = new Calculate.Application();
           app.RunFdScript(fdScript, false, true, true);
        }

        //Function for calculating the concrete volume from the bsv files
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
        //Function for calculating the reinforcement weight from the bsv files
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
        //Function for acessing utilisation from the bsv files
        public static double Utilisation()
        {
            //Read results from csv file
            double utilisation = 0;
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
                        utilisation = double.Parse(values[1], CultureInfo.InvariantCulture);
                    }
                    counter++;
                }
            }

            return utilisation;

        }
        //Function for acessing utilisation from the bsv files
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
        
    }
}