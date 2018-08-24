/**********************************************************************************************************************
 *                                                                          *
 * COPYRIGHT 2007 (C) BY ADVANCED MICRO-FABRICATION EQUIPMENT SHANGHAI INC. *
 188 Taihua Road, Jinqiao Export Processing Zone (South Area),         *
 *    Shanghai 201201, China                                                *
 * This source code is confidential, proprietary information of AMECN Inc.  *
 * and is an unpublished work of authorship protected by copyright laws.    *
 * Unauthorized copying or use of this document or the program contained,   *
 * in original or modified form, is a violation and maybe procecuted.       *
 *                      All Rights Reserved                                 *
 *                                                                          *
 *  File:          PJCJCtrl.cs                                                   *
 *  Author:		Kevin Xi                                                  *
 *  Content:    Process job and control job command handling implement file.    *
 *                                                                          *
 **********************************************************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;


using LinkEDA;
using AXLib;
using Tunnel;
using FALogger;
using CtrlJobPRJobDictionary;

using E40;
using E90;
using E94;
using ConcurrentCollections;
using CommonDef;

namespace PJCJCtrl
{
    public enum CtrlJobCmd
    {
        CJPause,
        CJResume,
        CJStop,
        CJAbort,
    }
    public class PJCJAdvancedCtrl 
    {
        //private static ConxCalls conectLinkEDA;
        //private static FALoggerClass FALogPJCJCtrl = new FALoggerClass();
        public void PRJobStopping(IProcessJob job)
        {
            //FALoggerClass.Instance.WriteFile("PRJobStopping[PJCJCtrl]", LogType.DEBUG, "IProcessJob job = " + job.objID);
            FALoggerClass.Instance.WriteFile("10149", LogType.DEBUG, " " + job.objID);
            if (job.PRJobState != PRJobState.PAUSING && job.PRJobState != PRJobState.PAUSED && job.PRJobState != PRJobState.NO_STATE)
            {
                DeleteWaferOrderList(job.Substrates, job.PRMtlNameList[0].CarrierID);
            }
            if (job.Substrates != null && job.Substrates.Length > 0)
            {
                SetSubstrateState(job.Substrates, SubstProcState.STOPPED);
            }
            else
            {
                //FALoggerClass.Instance.WriteFile("PRJobStopping[PJCJCtrl]", LogType.DEBUG, "Ijob.Substrates == null or  job.Substrates.Length == 0");
                FALoggerClass.Instance.WriteFile("10150", LogType.DEBUG," " );
            }
        }

        public void PRJobAborting(IProcessJob job)
        {
            //FALoggerClass.Instance.WriteFile("PRJobAborting[PJCJCtrl]", LogType.DEBUG, "IProcessJob job = " + job.objID);
            FALoggerClass.Instance.WriteFile("10151", LogType.DEBUG, " " + job.objID);

            if (job.Substrates != null && job.PRMtlNameList[0].CarrierID != null && job.ControlJobID != null)
            {
                if (job.PRJobState != PRJobState.PAUSING && job.PRJobState != PRJobState.PAUSED && job.PRJobState != PRJobState.NO_STATE)
                {
                    if ((int)TunnelClass.Instance.GetEC((int)EC_AX.EC_CJAbortRMMod, 0) == 0)
                        DeleteWaferOrderList(job.Substrates, job.PRMtlNameList[0].CarrierID);
                    else
                        ReturnWaferOrderList(job.Substrates, job.PRMtlNameList[0].CarrierID, job.ControlJobID, job.objID);
                }
                if (job.Substrates.Length > 0)
                {
                    SetSubstrateState(job.Substrates, SubstProcState.ABORTED);
                }
                int portId = TunnelClass.Instance.GetPortIdInCarrier(job.PRMtlNameList[0].CarrierID);
                if (portId != -1)
                {
                    TunnelClass.Instance.isReadyToSetFOUPDone(portId, true);
                }
                job.PRJobDone();
            }
            else
            {
                //FALoggerClass.Instance.WriteFile("PRJobAborting[PJCJCtrl]", LogType.DEBUG, "job.Substrates == null or job.PRMtlNameList[0].CarrierID == null or job.ControlJobID == null"); 
                FALoggerClass.Instance.WriteFile("10152", LogType.DEBUG," " );
            }
        }

        public void PRJobPaused(IProcessJob job)
        {
            //FALoggerClass.Instance.WriteFile("PRJobPaused[PJCJCtrl]", LogType.DEBUG, "IProcessJob job = " + job.objID);
            FALoggerClass.Instance.WriteFile("10153", LogType.DEBUG, " " + job.objID);
            if (job.Substrates != null && job.PRMtlNameList[0].CarrierID != null )
                DeleteWaferOrderList(job.Substrates, job.PRMtlNameList[0].CarrierID);
            else
                FALoggerClass.Instance.WriteFile("10154", LogType.DEBUG," " );
            //FALoggerClass.Instance.WriteFile("PRJobPaused[PJCJCtrl]", LogType.DEBUG, "job.Substrates == null or job.PRMtlNameList[0].CarrierID == null");
            job.PRJobPaused();

        }

        public void PRJobResumed(IProcessJob job)
        {
            //FALoggerClass.Instance.WriteFile("PRJobResumed[PJCJCtrl]", LogType.DEBUG, "IProcessJob job = " + job.objID);
            FALoggerClass.Instance.WriteFile("10155", LogType.DEBUG, " " + job.objID);
            if (job.Substrates != null && job.PRMtlNameList[0].CarrierID != null) 
                InsertWaferOrderList(job.Substrates, job.PRMtlNameList[0].CarrierID);
            else
                FALoggerClass.Instance.WriteFile("10156", LogType.DEBUG," " );
            //FALoggerClass.Instance.WriteFile("PRJobResumed[PJCJCtrl]", LogType.DEBUG, "job.Substrates == null or job.PRMtlNameList[0].CarrierID == null");
            job.PRJobResumed();
        }

        private void SetPRJobDone(IProcessJob job)
        {

        }

        private void SetSubstrateState(ISubstrate[] wafers, SubstProcState newState)
        {
            
            foreach (ISubstrate subst in wafers)
            {
                if (subst != null)
                {
                    //FALoggerClass.Instance.WriteFile("SetSubstrateState[PJCJCtrl]", LogType.DEBUG, "ISubstrate[] subst = " + subst.objID + "    " + "SubstProcState newState = " + newState.ToString());
                    FALoggerClass.Instance.WriteFile("10157", LogType.DEBUG, subst.objID +","+ newState.ToString());
                    if (subst.SubstProcState == SubstProcState.NEEDS_PROCESSING)
                        subst.SubstrateProcessStateChanged(newState);
                }
                else
                    FALoggerClass.Instance.WriteFile("10158", LogType.DEBUG," " );
                //FALoggerClass.Instance.WriteFile("SetSubstrateState[PJCJCtrl]", LogType.DEBUG, "subst == null");
            }
        }

        public void CtrlJobService(string sCtrlJobID, CtrlJobCmd cmd)
        {
            //FALoggerClass.Instance.WriteFile("CtrlJobService[PJCJCtrl]", LogType.DEBUG, "string sCtrlJobID = " + sCtrlJobID + "    " + "CtrlJobCmd cmd = " + cmd.ToString());
            FALoggerClass.Instance.WriteFile("10159", LogType.DEBUG, sCtrlJobID +","+ cmd.ToString());
            LinkedListNode<IProcessJob> aPJNode = ConxCalls.PRJOBLIST.First, nextNode = aPJNode;
            while (aPJNode != null)
            {
                nextNode = aPJNode.Next;
                if (aPJNode.Value.ControlJobID == sCtrlJobID)
                {
                    switch (cmd)
                    {
                        case CtrlJobCmd.CJPause:
                            break;

                        case CtrlJobCmd.CJResume:
                            break;

                        case CtrlJobCmd.CJStop:
                            //PRJobStopping(aPJNode.Value);
                            break;

                        case CtrlJobCmd.CJAbort:
                            //PRJobAborting(aPJNode.Value);
                            break;
                    }
                }
                aPJNode = nextNode;
            }           
        }

        private void DeleteWaferOrderList(ISubstrate[] wafers,string carrierid)
        {
            int portId, count = 0;
            int[] slotlist = new int[25];
            if (!TunnelClass.CarrierPortMap.ContainsKey(carrierid)) return;
            portId = TunnelClass.CarrierPortMap[carrierid] - 1;

            foreach (ISubstrate wafer in wafers)
            {
                if (wafer != null && (wafer.SubstState == SubstState.AT_SOURCE || wafer.SubstState == SubstState.AT_DESTINATION))
                {
                    slotlist[count] = Convert.ToInt32(wafer.CurrentSlot);
                    count++;
                }
                else
                    FALoggerClass.Instance.WriteFile("10160", LogType.INFO," " );
                //FALoggerClass.Instance.WriteFile("DeleteWaferOrderList[PJCJCtrl]", LogType.INFO, "wafer==null or  wafer.SubstState != SubstState.AT_SOURCE");
            }
            FALoggerClass.Instance.WriteFile("10161", LogType.INFO, " " + count.ToString());
            //FALoggerClass.Instance.WriteFile("DeleteWaferOrderList[PJCJCtrl]", LogType.INFO, "count = " + count.ToString());
            for (int i = 0; i < count; i++)
            {
                //Call AX function here one by one:  
                //FALoggerClass.Instance.WriteFile("DeleteWaferOrderList[PJCJCtrl]", LogType.INFO, "port = " + portId.ToString() + "   " + "slot = " + slotlist[i].ToString());
                FALoggerClass.Instance.WriteFile("10162", LogType.INFO, portId.ToString() +","+ slotlist[i].ToString());
                try
                {
                    TunnelClass.AXOBJFA.RemoveWaferOrderList(portId, slotlist[i], portId, slotlist[i], 0);
                }
                catch (System.Exception ex)
                {
                    //FALoggerClass.Instance.WriteFile("DeleteWaferOrderList[PJCJCtrl]", LogType.ERROR, "Catch exception. ex = " + ex.ToString());
                    FALoggerClass.Instance.WriteFile("10163", LogType.ERROR, " " + ex.ToString());
                }                
            }
        }

        private void ReturnWaferOrderList(ISubstrate[] wafers, string carrierid, string ctrlJobId, string prjobId)
        {
            int portId, count = 0;
            int waferBitMap = 0;
            portId = TunnelClass.CarrierPortMap[carrierid] - 1;
            waferBitMap = waferBitMap | (portId << 30);
            int[] slots = CJPJInfo.Instance.GetSlotsFromPRJob(ctrlJobId, prjobId);

            if (slots != null)
            {
                foreach (int slot in slots)
                {
                    if (slot != 0)
                    {
                        waferBitMap = waferBitMap | (1 << (slot - 1));
                        count++;
                    }
                }
                //FALoggerClass.Instance.WriteFile("ReturnWaferOrderList[PJCJCtrl]", LogType.INFO, "port = " + portId.ToString() + "   " + "waferBitMap = " + waferBitMap.ToString() + "count = " + count.ToString());
                FALoggerClass.Instance.WriteFile("10164", LogType.INFO, portId.ToString() +","+ waferBitMap.ToString() +","+ count.ToString());
                try
                {
                    TunnelClass.AXOBJFA.ReturnWaferOrderList(waferBitMap);
                }
                catch (System.Exception ex)
                {
                    //FALoggerClass.Instance.WriteFile("ReturnWaferOrderList[PJCJCtrl]", LogType.ERROR, "Catch exception. ex =  " + ex.ToString());
                    FALoggerClass.Instance.WriteFile("10165", LogType.ERROR, " " + ex.ToString());
                }                
            }
        }

        private void InsertWaferOrderList(ISubstrate[] wafers, string carrierid)
        {
            int portId, slotId = 0;
            int waferBitMap = 0, bitMask = 1;
            portId = TunnelClass.CarrierPortMap[carrierid] - 1;
            waferBitMap = waferBitMap | (portId << 30);
            foreach (ISubstrate wafer in wafers)
            {
                //FALoggerClass.Instance.WriteFile("InsertWaferOrderList[PJCJCtrl]", LogType.INFO, "SlotId = " + wafer.objID.ToString() + "    " + "SubProcState = " + wafer.SubstProcState.ToString());
                FALoggerClass.Instance.WriteFile("10166", LogType.INFO, wafer.objID.ToString() +","+ wafer.SubstProcState.ToString());
                if (wafer != null && wafer.SubstState == SubstState.AT_SOURCE)
                {
                    slotId = Convert.ToInt32(wafer.CurrentSlot);
                    waferBitMap = waferBitMap | (bitMask << slotId);
                    //FALoggerClass.Instance.WriteFile("InsertWaferOrderList[PJCJCtrl]", LogType.INFO, "port = " + portId.ToString());
                    FALoggerClass.Instance.WriteFile("10167", LogType.INFO, " " + portId.ToString());
                }
                else
                    FALoggerClass.Instance.WriteFile("10168", LogType.INFO," " );
                //FALoggerClass.Instance.WriteFile("InsertWaferOrderList[PJCJCtrl]", LogType.INFO, "wafer == null or  wafer.SubstState != SubstState.AT_SOURCE");
            }

            if ((waferBitMap & 0x3FFFFFFF) > 0)
            {
                try
                {
                    TunnelClass.AXOBJFA.InsertWaferOrderList(waferBitMap);
                    TunnelClass.AXOBJFA.StartProcess(portId, slotId);
                }
                catch (System.Exception ex)
                {
                    //FALoggerClass.Instance.WriteFile("InsertWaferOrderList[PJCJCtrl]", LogType.ERROR, "Catch exception. ex =  " + ex.ToString());
                    FALoggerClass.Instance.WriteFile("10169", LogType.ERROR, " " + ex.ToString());
                }

            }
        }
    }
}
