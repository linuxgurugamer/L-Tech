/*
 * L-Tech Scientific Industries Continued
 * Copyright © 2015-2018, Arne Peirs (Olympic1)
 * Copyright © 2016-2018, Jonathan Bayer (linuxgurugamer)
 * 
 * Kerbal Space Program is Copyright © 2011-2018 Squad. See https://kerbalspaceprogram.com/.
 * This project is in no way associated with nor endorsed by Squad.
 * 
 * This file is part of Olympic1's L-Tech (Continued). Original author of L-Tech is 'ludsoe' on the KSP Forums.
 * This file was not part of the original L-Tech but was written by Arne Peirs.
 * Copyright © 2015-2018, Arne Peirs (Olympic1)
 * 
 * Continues to be licensed under the MIT License.
 * See <https://opensource.org/licenses/MIT> for full details.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KSP.Localization;
using KSP.UI.Screens.Flight.Dialogs;
using LtScience.Utilities;
using LtScience.Windows;
using UnityEngine;

using static LtScience.Addon;

namespace LtScience.Modules
{
    // science is done at the resourceUsageRate per (???minute/hour/day????)
    // When the usedResource >= reqAmount,the experiment is complete

    // speed & efficiency of use based on kerbal's time in space
    // Need to come up with decent formula
    //static double NthRoot(double A, int N)
    //{
    //    return Math.Pow(A, 1.0 / N);
    //}

    public class Experiment
    {
        internal string name;
        internal string label;
        internal string tooltip;
        internal string neededResourceName;
        internal double resourceAmtRequired;
        internal double resourceUsageRate;
        internal float xmitDataScalar;
        internal uint biomeMask;
        internal uint situationMask;

        public Experiment(string name, string label, string tooltip, string neededResourceName, float resourceAmtRequired, float resourceUsageRate)
        {
            Init();
            this.name = name;
            this.label = label;
            this.tooltip = tooltip;
            this.neededResourceName = neededResourceName;
            this.resourceAmtRequired = resourceAmtRequired;
            this.resourceUsageRate = resourceUsageRate;
            this.biomeMask = 0;
            this.situationMask = 0;
        }

        protected internal Experiment()
        {
            Init();
        }
        void Init()
        {
            name = Localizer.Format("#LOC_lTech_80");
            label = Localizer.Format("#LOC_lTech_81");
            tooltip = Localizer.Format("#LOC_lTech_82");
            neededResourceName = Localizer.Format("#LOC_lTech_83");
        }
        internal Experiment(string id)
        {
            Init();
            name = id;
        }
        public Experiment Load(string id, ConfigNode node)
        {
            var experiment = new Experiment(id);

            node.TryGetValue(Localizer.Format("#LOC_lTech_84"), ref experiment.label);
            node.TryGetValue(Localizer.Format("#LOC_lTech_85"), ref experiment.tooltip);
            node.TryGetValue(Localizer.Format("#LOC_lTech_86"), ref experiment.neededResourceName);
            node.TryGetValue(Localizer.Format("#LOC_lTech_87"), ref experiment.resourceAmtRequired);
            node.TryGetValue(Localizer.Format("#LOC_lTech_88"), ref experiment.xmitDataScalar);
            node.TryGetValue(Localizer.Format("#LOC_lTech_89"), ref experiment.resourceUsageRate);
#if false
            Log.Info(Localizer.Format("#LOC_lTech_90") + experiment.name +
                Localizer.Format("#LOC_lTech_91") + experiment.label +
                Localizer.Format("#LOC_lTech_92") + experiment.tooltip +
                Localizer.Format("#LOC_lTech_93") + experiment.neededResourceName +
                Localizer.Format("#LOC_lTech_94") + experiment.resourceAmtRequired +
                Localizer.Format("#LOC_lTech_95") + resourceUsageRate);
#endif
            return experiment;
        }
    }

    internal class ExpStatus
    {
        internal const string EXPERIMENT_STATUS = "ExpStatus";

        internal string expId;
        internal string key;
        internal string bodyName;
        internal ExperimentSituations vesselSit;
        internal string biome;
        internal double processedResource;
        internal string reqResource;
        internal double reqAmount;
        internal bool active;
        internal double lastTimeUpdated;


        public ExpStatus(string expId, string key, string bodyName, ExperimentSituations vesselSit, string biome, string reqResource, double reqAmount)
        {
            this.expId = expId;
            this.key = key;
            this.bodyName = bodyName;
            this.vesselSit = vesselSit;
            this.processedResource = 0;
            this.biome = biome;
            this.reqResource = reqResource;
            this.reqAmount = reqAmount;
            active = false;
            lastTimeUpdated = 0;
        }

        protected internal ExpStatus() // internal use only
        {

            expId = Localizer.Format("#LOC_lTech_96");
            key = Localizer.Format("#LOC_lTech_97");
            bodyName = Localizer.Format("#LOC_lTech_98");
            reqResource = Localizer.Format("#LOC_lTech_99");
            biome = Localizer.Format("#LOC_lTech_100");
            this.processedResource = 0;
            active = false;
        }
        public string Key
        {
            get
            {
                return key;
            }
        }
        public static ExpStatus Load(ConfigNode node, SkylabExperiment instance = null)
        {
            var expStatus = new ExpStatus();

            node.TryGetValue(Localizer.Format("#LOC_lTech_101"), ref expStatus.expId);
            node.TryGetValue(Localizer.Format("#LOC_lTech_102"), ref expStatus.key);
            node.TryGetValue(Localizer.Format("#LOC_lTech_103"), ref expStatus.bodyName);
            node.TryGetEnum<ExperimentSituations>(Localizer.Format("#LOC_lTech_104"), ref expStatus.vesselSit, 0);
            node.TryGetValue(Localizer.Format("#LOC_lTech_105"), ref expStatus.biome);
            node.TryGetValue(Localizer.Format("#LOC_lTech_106"), ref expStatus.processedResource);
            node.TryGetValue(Localizer.Format("#LOC_lTech_107"), ref expStatus.reqResource);
            node.TryGetValue(Localizer.Format("#LOC_lTech_108"), ref expStatus.reqAmount);
            node.TryGetValue(Localizer.Format("#LOC_lTech_109"), ref expStatus.active);
            node.TryGetValue(Localizer.Format("#LOC_lTech_110"), ref expStatus.lastTimeUpdated);
            if (expStatus.active)
            {
                ModuleScienceExperiment exp = new ModuleScienceExperiment();
                exp.experimentID = expStatus.expId;
                if (experiments[expStatus.expId].xmitDataScalar > 0)
                    exp.xmitDataScalar = experiments[expStatus.expId].xmitDataScalar;

                if (instance != null)
                    instance.SetUpActiveExperiment(expStatus.expId, expStatus.biome, exp, expStatus.reqResource, expStatus.processedResource);
            }
#if false
            Log.Info(Localizer.Format("#LOC_lTech_111") + expStatus.expId + Localizer.Format("#LOC_lTech_112") + expStatus.key + Localizer.Format("#LOC_lTech_113") + expStatus.bodyName +
                Localizer.Format("#LOC_lTech_114") + expStatus.vesselSit + Localizer.Format("#LOC_lTech_115") + expStatus.biome + Localizer.Format("#LOC_lTech_116") + expStatus.processedResource +
                Localizer.Format("#LOC_lTech_117") + expStatus.reqAmount + Localizer.Format("#LOC_lTech_118") + expStatus.active);
#endif
            return expStatus;
        }

        public void Save(ConfigNode node)
        {
            node.SetValue(Localizer.Format("#LOC_lTech_119"), expId, true);
            node.SetValue(Localizer.Format("#LOC_lTech_120"), key, true);
            node.SetValue(Localizer.Format("#LOC_lTech_121"), bodyName, true);
            node.SetValue(Localizer.Format("#LOC_lTech_122"), vesselSit.ToString(), true);
            node.SetValue(Localizer.Format("#LOC_lTech_123"), biome, true);
            node.SetValue(Localizer.Format("#LOC_lTech_124"), processedResource, true);
            node.SetValue(Localizer.Format("#LOC_lTech_125"), reqResource, true);
            node.SetValue(Localizer.Format("#LOC_lTech_126"), reqAmount, true);
            node.SetValue(Localizer.Format("#LOC_lTech_127"), active, true);
            node.SetValue(Localizer.Format("#LOC_lTech_128"), lastTimeUpdated, true);
        }
    }


    internal class ActiveExperiment
    {
        internal string activeExpid;
        internal string bodyName;
        internal ExperimentSituations expSit;
        internal string biomeSit;
        internal ModuleScienceExperiment mse;
        internal bool completed;
        internal ActiveExperiment(string expId, string bodyName, ExperimentSituations sit, string biomeSit, ModuleScienceExperiment mse)
        {
            Init(expId, bodyName, sit, biomeSit, mse);
        }
        internal ActiveExperiment(string expId, string bodyName, ExperimentSituations sit, string biomeSit)
        {
            Init(expId, bodyName, sit, biomeSit, null);
        }
        void Init(string expId, string bodyName, ExperimentSituations sit, string biomeSit, ModuleScienceExperiment mse)
        {
            this.activeExpid = expId;
            this.bodyName = bodyName;
            this.expSit = sit;
            this.biomeSit = biomeSit;
            this.mse = mse;
            completed = false;
        }

        public string KeyUnpacked(string expid)
        {
            string expSit = this.expSit.ToString();
            string biomeSit = this.biomeSit;

            uint e_sit = Addon.experiments[expid].situationMask;
            uint e_biome = Addon.experiments[expid].biomeMask;


            if (e_sit > 0)
            {
                if ((e_sit & (uint)this.expSit) == 0)
                {
                    return null;
                }
            }
            else
                expSit = Localizer.Format("#LOC_lTech_129");

            if (e_biome > 0)
            {
                if ((e_biome & (uint)this.expSit) == 0)
                {
                    return null;
                }
            }
            else
                biomeSit = Localizer.Format("#LOC_lTech_130");


            return expid + Localizer.Format("#LOC_lTech_131") + bodyName + Localizer.Format("#LOC_lTech_132") + expSit.ToString() + Localizer.Format("#LOC_lTech_133") + biomeSit;

        }
        public string Key
        {
            get
            {
                return KeyUnpacked(activeExpid);
            }
        }

#if false
            public static bool operator ==(ActiveExperiment lhs, ActiveExperiment rhs)
            {
                if ((object)lhs == null || (object)rhs == null)
                    return false;
                return (lhs.activeExpid == rhs.activeExpid &&
                    lhs.bodyName == rhs.bodyName &&
                    lhs.expSit == rhs.expSit &&
                    lhs.biomeSit == rhs.biomeSit);
            }
            public static bool operator !=(ActiveExperiment lhs, ActiveExperiment rhs)
            {
                if ((object)lhs == null || (object)rhs == null)
                    return true;
                return (lhs.activeExpid != rhs.activeExpid ||
                    lhs.bodyName != rhs.bodyName ||
                    lhs.expSit != rhs.expSit ||
                    lhs.biomeSit != rhs.biomeSit);
            }
#endif
    }

    public class SkylabExperiment : PartModule, IScienceDataContainer
    {

        internal class Body
        {
            internal CelestialBody _celestialBody;
            public Body(CelestialBody Body)
            {
                _celestialBody = Body;
            }
        }

        internal Dictionary<CelestialBody, Body> _bodyList = new Dictionary<CelestialBody, Body>();



        const string SCIENCE_DATA = "ScienceData";
        const string EXPERIMENT = "EXPERIMENT";

        internal WindowSkylab windowSkylab = null;
        internal ActiveExperiment activeExperiment = null;
        internal static bool experimentStarted = false;

        SkylabCore skylabcoreModule = null;

        #region Properties

        [KSPField]
        public float labBoostScalar = 1f;

        private readonly List<ScienceData> _storedData = new List<ScienceData>();
        internal Dictionary<string, ExpStatus> expStatuses = new Dictionary<string, ExpStatus>();
        private ExperimentsResultDialog _expDialog;

        #endregion

        #region Event Handlers

        public new void Awake()
        {
            base.Awake();
            GameEvents.onGamePause.Add(OnPause);
            GameEvents.onGameUnpause.Add(OnUnpause);
            activeExperiment = null;
        }

        public override void OnSave(ConfigNode node)
        {
            foreach (ScienceData data in _storedData)
            {
                data.Save(node.AddNode(SCIENCE_DATA));
            }
            foreach (var data in expStatuses.Values)
            {
                data.Save(node.AddNode(ExpStatus.EXPERIMENT_STATUS));
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            Log.Info(Localizer.Format("#LOC_lTech_134"));
            _storedData.Clear();
            expStatuses.Clear();

            if (node.HasNode(SCIENCE_DATA))
            {
                foreach (ConfigNode dataNode in node.GetNodes(SCIENCE_DATA))
                {
                    _storedData.Add(new ScienceData(dataNode));
                }
            }


            if (node.HasNode(ExpStatus.EXPERIMENT_STATUS))
            {
                foreach (ConfigNode dataNode in node.GetNodes(ExpStatus.EXPERIMENT_STATUS))
                {
                    var data = ExpStatus.Load(dataNode, this);
                    if (!expStatuses.ContainsKey(data.Key))
                        expStatuses.Add(data.Key, data);
                }
            }


            Log.Info(Localizer.Format("#LOC_lTech_135") + experiments.Count);
        }

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                Log.Info(Localizer.Format("#LOC_lTech_136"));
                skylabcoreModule = part.FindModuleImplementing<SkylabCore>();
                StartCoroutine(Localizer.Format("#LOC_lTech_137"));
            }
        }
        IEnumerator SlowUpdate()
        {
            while (true)
            {
                UpdateUI();
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        public void OnDestroy()
        {
            Log.Info(Localizer.Format("#LOC_lTech_138"));
            StopCoroutine(Localizer.Format("#LOC_lTech_139"));
            GameEvents.onGamePause.Remove(OnPause);
            GameEvents.onGameUnpause.Remove(OnUnpause);
        }

        private void UpdateUI()
        {
            //Events["OpenGui"].active = true;
            Events[Localizer.Format("#LOC_lTech_140")].active = _storedData.Count > 0;
            Events[Localizer.Format("#LOC_lTech_141")].active = _storedData.Count > 0;

            Events[Localizer.Format("#LOC_lTech_142")].guiActive =
                (skylabcoreModule != null &&
                 part.protoModuleCrew.Count >= skylabcoreModule.minimumCrew &&
                 !Utils.CheckBoring(vessel) &&
                 skylabcoreModule.GetCrewScientistTotals() >= skylabcoreModule.minCrewScienceExp);
        }

        private void OnPause()
        {
            if (_expDialog != null)
                _expDialog.gameObject.SetActive(false);
        }

        private void OnUnpause()
        {
            if (_expDialog != null)
                _expDialog.gameObject.SetActive(true);
        }

        #endregion

        #region KSP Events


        [KSPEvent(active = true, guiActive = true, guiName = "#autoLOC_LTech_Experiment_001")] // Open experiment GUI
        public void OpenGui()
        {
            ToggleGUI();
        }

        internal void ToggleGUI()
        {
            if (windowSkylab == null)
            {
                windowSkylab = gameObject.AddComponent<WindowSkylab>();
                windowSkylab.LabExp = this;
                Events[Localizer.Format("#LOC_lTech_143")].guiName = Localizer.Format("#LOC_lTech_144");
            }
            else
            {
                Events[Localizer.Format("#LOC_lTech_145")].guiName = Localizer.Format("#LOC_lTech_146");
                Destroy(windowSkylab);
                windowSkylab = null;

            }
        }



        [KSPEvent(active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, guiName = "#autoLOC_6004057", unfocusedRange = 2f)]
        public void EvaCollect()
        {
            List<ModuleScienceContainer> evaCont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();

            if (_storedData.Count > 0)
            {
                if (evaCont.First().StoreData(new List<IScienceDataContainer> { this }, false))
                {
                    foreach (ScienceData data in _storedData)
                        DumpData(data);
                }
            }
        }

        [KSPEvent(active = true, guiActive = true, guiName = "#autoLOC_502204")]
        public void ReviewDataEvent()
        {
            ReviewData();
            //UpdateUI();
        }

        #endregion
        internal static int GetResourceID(string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return resource.id;
        }

        #region UnityStuff
        Double lastUpdateTime = 0;
        IEnumerator FixedUpdate2()
        {
            while (activeExperiment != null)
            {
                yield return new WaitForSecondsRealtime(1);
                Do_SlowUpdate();
            }
        }
        public void Do_SlowUpdate()
        {
            if (activeExperiment != null)
            {
                double curTime = Planetarium.GetUniversalTime();
                double delta = curTime - lastUpdateTime;

                // Tasks
                // 1. Make sure experiment situation hasn't changed, if it has, then return
                // 2. calcualte resource usage
                // 3. Check to see if exeriment is completed, if so, set a flag

                string biome, displayBiome;
                if (vessel.landedAt != string.Empty)
                {
                    biome = Vessel.GetLandedAtString(vessel.landedAt);
                    displayBiome = Localizer.Format(vessel.displaylandedAt);
                }
                else
                {
                    biome = ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude);
                    displayBiome = ScienceUtil.GetBiomedisplayName(vessel.mainBody, biome);
                }

                var curExp = new ActiveExperiment(activeExperiment.activeExpid, vessel.mainBody.bodyName, ScienceUtil.GetExperimentSituation(vessel), biome);

                if ((object)curExp != null && curExp.Key == activeExperiment.Key)
                {
                    if (!expStatuses.ContainsKey(activeExperiment.Key))
                    {
                        Log.Error("Key missing from expStatuses, key: " + activeExperiment.Key);
                        foreach (var e in expStatuses.Keys)
                            Log.Error("key: " + e);
                    }
                    double resourceRequest = delta / Planetarium.fetch.fixedDeltaTime;

                    double amtNeeded = Math.Min(
                        experiments[activeExperiment.activeExpid].resourceUsageRate * resourceRequest,
                         experiments[activeExperiment.activeExpid].resourceAmtRequired - expStatuses[activeExperiment.Key].processedResource);
                    amtNeeded = amtNeeded * KCT_Interface.ResearchTimeAdjustment();

                    //Log.Info("SkylabExperiment, amtNeeded: " + amtNeeded.ToString("F3") + ",  activeExperiment.Key: " + activeExperiment.Key +
                    //    ", processedResource: " + expStatuses[activeExperiment.Key].processedResource +
                    //    ", resourceAmtRequired: " + Addon.experiments[activeExperiment.activeExpid].resourceAmtRequired);

                    double resource = part.RequestResource(experiments[activeExperiment.activeExpid].neededResourceName, amtNeeded);
                    expStatuses[activeExperiment.Key].processedResource += resource;

                    int resourceID = GetResourceID(expStatuses[activeExperiment.Key].reqResource);
                    //                    part.GetConnectedResourceTotals(resourceID, out double amount, out double maxAmount);

                    expStatuses[activeExperiment.Key].lastTimeUpdated = Planetarium.GetUniversalTime();

                    if (HighLogic.CurrentGame.Parameters.CustomParams<LTech_1>().exitWarpWhenDone)
                    {
                        var percent = 0.001 + expStatuses[activeExperiment.Key].processedResource / Addon.experiments[activeExperiment.activeExpid].resourceAmtRequired * 100;
                        if (percent >= 100f)
                        {
                            if (TimeWarp.CurrentRateIndex > 0)
                            {
                                StartCoroutine(CancelWarp());
                                //TimeWarp.fetch.CancelAutoWarp();
                                //TimeWarp.SetRate(0, true);
                            }
                        }
                    }
                    // var experiment = experiments[activeExperiment.activeExpid];
                }
                else
                {
                    Log.Info(Localizer.Format("#LOC_lTech_147"));
                    Utils.DisplayScreenMsg(Localizer.Format("#LOC_lTech_148"));
                } // need to decide what to do if something changed
                lastUpdateTime = curTime;

            }
            else
                if (experimentStarted)
                    Log.Info(Localizer.Format("#LOC_lTech_149"));

        }

        IEnumerator CancelWarp()
        {
            TimeWarp w = TimeWarp.fetch;
            TimeWarp.fetch.CancelAutoWarp();
            while (w.current_rate_index > 0)
            {
                Log.Info(Localizer.Format("#LOC_lTech_150"));
                //Make sure we cancel autowarp if its engaged
                TimeWarp.SetRate(w.current_rate_index - 1, true);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            yield return null;
        }
        #endregion
        #region Science

        internal void DoScience(string expId)
        {
            string step = Localizer.Format("#LOC_lTech_151");

            string reqResource = experiments[expId].neededResourceName;
            float reqAmount = 1;

            try
            {
                string msg = string.Empty;
                //Vessel ves = FlightGlobals.ActiveVessel;
                Part prt = FlightGlobals.ActiveVessel.rootPart;
                ModuleScienceExperiment exp = new ModuleScienceExperiment();
                if (experiments[expId].xmitDataScalar > 0)
                    exp.xmitDataScalar = experiments[expId].xmitDataScalar;
                Log.Info(Localizer.Format("#LOC_lTech_152") + expId + Localizer.Format("#LOC_lTech_153") + exp.xmitDataScalar);

                // Checks
                step = Localizer.Format("#LOC_lTech_154");
                if (Utils.CheckBoring(vessel, true))
                    return;

                step = Localizer.Format("#LOC_lTech_155");
                if (!Utils.CanRunExperiment(vessel, expId, ref msg))
                {
                    Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_LTech_Experiment_002", msg));
                    return;
                }

#if false
                step = Localizer.Format("#LOC_lTech_156");
                if (Utils.ResourceAvailable(prt, Localizer.Format("#LOC_lTech_157")) < reqInsight)
                {
                    double current = Utils.ResourceAvailable(prt, Localizer.Format("#LOC_lTech_158"));
                    double needed = reqInsight - current;

                    Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_LTech_Experiment_003", (int)needed));
                    return;
                }
#endif

                step = Localizer.Format("#LOC_lTech_159");
                if (Utils.ResourceAvailable(prt, reqResource) < reqAmount)
                {
                    double current = Utils.ResourceAvailable(prt, reqResource);
                    double needed = reqAmount - current;

                    Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_LTech_Experiment_004", (int)needed, reqResource));
                    return;
                }

                // Experiment
                step = Localizer.Format("#LOC_lTech_160");
                exp.experimentID = expId;
                ScienceExperiment labExp = ResearchAndDevelopment.GetExperiment(exp.experimentID);
                if (labExp == null)
                {
                    Log.Warning(Localizer.Format("#autoLOC_LTech_Experiment_005", exp.experimentID));
                    Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_LTech_Experiment_006"));
                    return;
                }

                step = Localizer.Format("#LOC_lTech_161");
                ExperimentSituations vesselSit = ScienceUtil.GetExperimentSituation(vessel);
                if (labExp.IsAvailableWhile(vesselSit, vessel.mainBody))
                {
                    step = Localizer.Format("#LOC_lTech_162");
                    string biome, displayBiome;
                    if (vessel.landedAt != string.Empty)
                    {
                        biome = Vessel.GetLandedAtString(vessel.landedAt);
                        displayBiome = Localizer.Format(vessel.displaylandedAt);
                    }
                    else
                    {
                        biome = ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude);
                        displayBiome = ScienceUtil.GetBiomedisplayName(vessel.mainBody, biome);
                    }

                    Log.Info(Localizer.Format("#LOC_lTech_163") + expId +
                        Localizer.Format("#LOC_lTech_164") + vessel.mainBody.bodyName +
                        Localizer.Format("#LOC_lTech_165") + ScienceUtil.GetExperimentSituation(vessel) +
                        Localizer.Format("#LOC_lTech_166") + biome);

                    SetUpActiveExperiment(expId, biome, exp, reqResource);
#if false
                    activeExperiment = new ActiveExperiment(expId, vessel.mainBody.bodyName, ScienceUtil.GetExperimentSituation(vessel), biome, exp);

                    // need to add to 
                    //                   internal ExperimentSituations vesselSit;
                    //                    internal string biome;
                    ExpStatus es = new ExpStatus(expId, activeExperiment.Key, vessel.mainBody.bodyName, ScienceUtil.GetExperimentSituation(vessel), biome,
                        reqResource, experiments[expId].resourceAmtRequired);
                    es.active = true;
                    expStatuses.Add(es.Key, es);
                    experimentStarted = true;
                    StartCoroutine(FixedUpdate2());
#endif
                }
                else
                {
                    Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_238424", labExp.experimentTitle));
                    Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_LTech_Experiment_007", vesselSit.displayDescription()));
                }

                step = Localizer.Format("#LOC_lTech_167");
            }
            catch (Exception ex)
            {
                Log.Error($"SkylabExperiment.DoScience at step \"{step}\";. Error: {ex}");
            }
        }


        internal void SetUpActiveExperiment(string expId, string biome, ModuleScienceExperiment exp, string reqResource, double processedResource = 0)
        {

            activeExperiment = new ActiveExperiment(expId, vessel.mainBody.bodyName, ScienceUtil.GetExperimentSituation(vessel), biome, exp);

            ExpStatus es = new ExpStatus(expId, activeExperiment.Key, vessel.mainBody.bodyName, ScienceUtil.GetExperimentSituation(vessel), biome,
                reqResource, experiments[expId].resourceAmtRequired);
            es.processedResource = processedResource;
            es.active = true;
            expStatuses.Add(es.Key, es);
            experimentStarted = true;

            lastUpdateTime = Planetarium.GetUniversalTime();

            StartCoroutine(FixedUpdate2());

        }

        internal void FinalizeExperiment()
        {
            Log.Info(Localizer.Format("#LOC_lTech_168"));

            ScienceExperiment labExp = ResearchAndDevelopment.GetExperiment(activeExperiment.activeExpid);


            string displayBiome = Localizer.Format("#LOC_lTech_169");
            if (vessel.landedAt != string.Empty)
            {
                activeExperiment.biomeSit = Vessel.GetLandedAtString(vessel.landedAt);
                displayBiome = Localizer.Format(vessel.displaylandedAt);
            }
            else
            {
                activeExperiment.biomeSit = ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude);
                displayBiome = ScienceUtil.GetBiomedisplayName(vessel.mainBody, activeExperiment.biomeSit);

                activeExperiment.biomeSit = Localizer.Format("#LOC_lTech_170");
                displayBiome = Localizer.Format("#LOC_lTech_171");
            }

            ModuleScienceExperiment exp = activeExperiment.mse;

#if DEBUG
            var step = Localizer.Format("#LOC_lTech_172");
#endif
            ScienceSubject labSub = ResearchAndDevelopment.GetExperimentSubject(labExp, activeExperiment.expSit, vessel.mainBody, activeExperiment.biomeSit, displayBiome);
            //labSub.title = $"{labExp.experimentTitle}";
            if (activeExperiment.biomeSit != Localizer.Format("#LOC_lTech_173"))
                labSub.title = ScienceUtil.GenerateScienceSubjectTitle(labExp, activeExperiment.expSit, vessel.mainBody, activeExperiment.biomeSit, displayBiome);
            else
                labSub.title = ScienceUtil.GenerateScienceSubjectTitle(labExp, activeExperiment.expSit, vessel.mainBody);


            //labSub.subjectValue *= labBoostScalar;
            labSub.scienceCap = labExp.scienceCap * labSub.subjectValue;

#if DEBUG
            step = Localizer.Format("#LOC_lTech_174");
#endif
            float sciencePoints = labExp.baseValue * labExp.dataScale;

            ScienceData labData = new ScienceData(sciencePoints, exp.xmitDataScalar, 0, labSub.id, labSub.title, false, vessel.rootPart.flightID);

#if DEBUG
            step = Localizer.Format("#LOC_lTech_175");
#endif
            _storedData.Add(labData);

#if DEBUG
            step = Localizer.Format("#LOC_lTech_176");
#endif
            Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_238419", vessel.rootPart.partInfo.title, labData.dataAmount, labSub.title));
            ReviewDataItem(labData);

            expStatuses.Remove(activeExperiment.Key);
            activeExperiment = null;
            labData = null;
        }
        #endregion

        #region Result Dialog

        private void ShowResultDialog(ScienceData data)
        {
            Log.Info(Localizer.Format("#LOC_lTech_177"));
            ScienceLabSearch labSearch = new ScienceLabSearch(FlightGlobals.ActiveVessel, data);

            _expDialog = ExperimentsResultDialog.DisplayResult(new ExperimentResultDialogPage(
                FlightGlobals.ActiveVessel.rootPart,
                data,
                data.baseTransmitValue,
                data.transmitBonus,
                false,
                string.Empty,
                false,
                labSearch,
                OnDiscardData,
                OnKeepData,
                OnTransmitData,
                OnSendToLab));

            ScienceSubject subjectByID = ResearchAndDevelopment.GetSubjectByID(data.subjectID);
            var refValue = ResearchAndDevelopment.GetReferenceDataValue(data.dataAmount, subjectByID) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
            var scienceValue = ResearchAndDevelopment.GetScienceValue(data.dataAmount, data.scienceValueRatio, subjectByID, 1f) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

            //Log.Info("ShowResultDialog, data: " + data.title + ", labValue: " + data.labValue + ", dataAmount: " + data.dataAmount + ", scienceValueRatio: " + data.scienceValueRatio  + " ,baseTransmitValue: " + data.baseTransmitValue + ", transmitBonus: " + data.transmitBonus + " ::: data.subjectID: " + data.subjectID + ", data.dataAmount: " + data.dataAmount + ", subjectByID: " + subjectByID.id + ", subjectByID.dataScale: " + subjectByID.dataScale + ", subjectByID.subjectValue: " + subjectByID.subjectValue + ", HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier: " + HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier +  ", refValue: " + refValue + ", scienceValue: " + scienceValue);
        }

        public void ReviewData()
        {
            if (_storedData.Count == 0)
                return;
            Log.Info(Localizer.Format("#LOC_lTech_178"));
            foreach (ScienceData data in _storedData)
                ReviewDataItem(data);
        }

        public void ReviewDataItem(ScienceData data)
        {
            Log.Info(Localizer.Format("#LOC_lTech_179"));
            ShowResultDialog(data);
        }

        private void OnDiscardData(ScienceData data)
        {
            _expDialog = null;
            DumpData(data);
            //UpdateUI();
        }

        private void OnKeepData(ScienceData data)
        {
            _expDialog = null;
            //UpdateUI();
        }

        private void OnTransmitData(ScienceData data)
        {
            _expDialog = null;
            IScienceDataTransmitter transmitter = ScienceUtil.GetBestTransmitter(FlightGlobals.ActiveVessel);

            if (transmitter != null)
            {
                transmitter.TransmitData(new List<ScienceData> { data });
                DumpData(data);
            }
            else if (CommNet.CommNetScenario.CommNetEnabled)
            {
                Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_237738"));
            }
            else
            {
                Utils.DisplayScreenMsg(Localizer.Format("#autoLOC_237740"));
            }

            //UpdateUI();
        }

        private void OnSendToLab(ScienceData data)
        {
            _expDialog = null;
            ScienceLabSearch labSearch = new ScienceLabSearch(FlightGlobals.ActiveVessel, data);

            if (labSearch.NextLabForDataFound)
            {
                StartCoroutine(labSearch.NextLabForData.ProcessData(data));
                DumpData(data);
            }
            else
            {
                labSearch.PostErrorToScreen();
                //UpdateUI();
            }
        }

        #endregion

        #region IScienceDataContainer

        public ScienceData[] GetData()
        {
            return _storedData.ToArray();
        }

        public int GetScienceCount()
        {
            return _storedData.Count;
        }

        public bool IsRerunnable()
        {
            return true;
        }

        public void ReturnData(ScienceData data)
        {
            if (data == null)
                return;

            _storedData.Add(data);
            //UpdateUI();
        }

        public void DumpData(ScienceData data)
        {
            _storedData.Remove(data);
            //UpdateUI();
        }

        #endregion
    }
}
