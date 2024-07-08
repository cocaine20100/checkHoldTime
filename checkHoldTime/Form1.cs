using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DBControler;
using System.IO;


namespace checkHoldTime
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            checkHoldTime_PCB7();
        }

        string conn = System.Configuration.ConfigurationManager.AppSettings["cnMES"];
        string connRun = System.Configuration.ConfigurationManager.AppSettings["cnRun"];
        public void checkHoldTime_PCB7()
        {
            OleDB db = new OleDB();
            DataSet dsHIST = new DataSet();
            DataSet dsCHK = new DataSet();
            DataSet dsCHK2 = new DataSet();
            DataSet dsRULE = new DataSet();
            DataTable dtHist = new DataTable();
            DataTable dtRULE = new DataTable();
            string aTo = "";
            string aCc = "";
            string bCc = "";
            int rtn;
            
            string PRODAREA = "PCB7";
            string RUNFORM = "RUNCARD3";
            string CHKHOLD = "";
            string LIGHT, R_stage;
            string aSubject = "山鶯廠 QUEUETIME預警/超時通知";
            string str_LOTID="";
            string OM;
            DataSet dsOM = new DataSet();


            StringBuilder BODY = new StringBuilder();
            db.setConn(conn);
            string sql = "";

            try
            {
                //2024-06-21 mark
                //sql = " delete  from HOLDTIME_HOLD t where PAREA='" + PRODAREA + "' and t.lights='2' "; //and PARTID like '14VQWW136KOCQ % '
                //db.setConn(conn);
                //rtn = db.ExcuteNonQuery(sql);
                sql = @"SELECT LOTID, PARTID, LOTTYPE, STARTTIME, to_char(starttime, 'mm/dd')  CRtime,STATE,LASTEVTIME,CR2,
                    CURMAINQTY,REQDTIME,LOCATION,RECPID,REPLACE(REPLACE(STAGE, 'ST1', 'OU1'), 'SU1', 'OU1') as STAGE,MSAP,
                    MAINMATTYPE FROM DGV22.ACTL A, runcard@run.world B 
                    where A.PRODAREA = '" + PRODAREA + @"' AND A.COMCLASS = 'W' and A.STATE in ('D','I','J','E','K') 
                    and B.PRODAREA='" + PRODAREA + @"' and substr(A.Partid,1,instr(A.Partid,'-')-2)=B.料號 
                    and LOTTYPE in ('P1','P2')  and LOCATION<>'7FQC'    and LOTID  not in ('727010008.1')   and PARTID not like '10VPD7098EA%'  
                       ";
                // and STAGE='IN3'  and LOTID ='792008807.1' and PARTID like '14VQWW136KOCQ % '
                sql += " and lotid='745011602.1'";
                sql += " order by PARTID,STAGE,LOTID";
                db.setConn(conn);
                dsHIST = db.ExcuteDataSet(sql);
                BODY.Append("<body>");
                BODY.Append("<a href='http://10.14.65.71/mes/CusMenu/LocWip/holdtime_Kanban_Q.asp?Groupid=7&LOCATION=7AOI'>QUEUE TIME看板網址</a>");
                BODY.Append("<table border=1>");
                BODY.Append("<tr><td>工令</td><td>料號</td><td>批號</td><td>STAGE</td><td>現在LOCATION</td><td>現在RECP</td><td>現在狀態</td><td>規則描述</td><td>管制起製程</td><td>管制迄製程</td>");
                BODY.Append("<td>經過時間</td><td>ALERTTIME</td><td>QUEUETIME</td><td>規則狀況</td><td>燈號</td><td>數量</td><td>單位</td></tr>");
                //跑所有WIP工令
                string state, RULEMEMO, mSAP, oldPART, oldSTAGE, RUNTIME, RULETIME;
                string str_PARTID, str_STAGE;
                mSAP = "";
                oldPART = "";
                oldSTAGE = "";
                if (dsHIST.Tables[0].Rows.Count > 0)
                {
                    dtHist = dsHIST.Tables[0];
                    for (int i = 0; i < dtHist.Rows.Count; i++)
                    {
                        str_LOTID = dtHist.Rows[i]["LOTID"].ToString();
                        str_PARTID = dtHist.Rows[i]["PARTID"].ToString();
                        str_STAGE = dtHist.Rows[i]["STAGE"].ToString();
                        CHKHOLD = "YES";
                        state = "";
                        RULEMEMO = "";
                        //20180822 增加IN1不判斷VIA的版子	
                        if (str_STAGE == "IN1")
                        {
                            sql = "select * from BSIDE t where t.料號 ='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + "' and STAGE='IN1' and 製程別='E0114' ";
                            db.setConn(connRun);
                            dsCHK = db.ExcuteDataSet(sql);
                            if (dsCHK.Tables[0].Rows.Count > 0) //20180822 增加IN1不判斷VIA的版子
                                continue;// CHKHOLD = "NO";
                            //else
                            //    CHKHOLD = "YES";
                        }

                        //if (CHKHOLD == "YES")//20180822 增加IN1不判斷VIA的版子
                        //{
                        //mark
                            //sql = @"DELETE HOLDTIME_CHKLOG where PAREA='" + PRODAREA + "' and LOTID='" + str_LOTID + @"' 
                            //    and REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' 
                            //    and rulememo<>'規則符合-已在迄製程下過機台' and rulememo<>'規則符合-已在迄製程上過機台' and 
                            //    rulememo<>'規則不符合-已在迄製程下過機台' and rulememo<>'規則不符合-已在迄製程上過機台' ";
                            //db.setConn(conn);
                            //rtn = db.ExcuteNonQuery(sql);

                            if (oldPART == str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) &&
                                oldSTAGE == str_STAGE)
                            { }
                            else
                            {
                                //判斷該PART SATGE是否有MSAP
                                sql = " select STAGE,LOCATION from ROUT where PARTID like '" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"%' 
                                    and STAGE='" + str_STAGE + "' and LOCATION='7M_INNER1' ";
                                db.setConn(conn); //2024-07-08
                                dsCHK = db.ExcuteDataSet(sql);
                                if (dsCHK.Tables[0].Rows.Count > 0)
                                    mSAP = "NEED";
                                else
                                    mSAP = "NONEED";

                                //2024-07-05 判斷是否為OM料號 2024-07-08
                                sql = "select * from "+ RUNFORM + " where 料號 like '" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"%'  and OPTICAL_M='ON'";
                                db.setConn(connRun);
                                dsOM = db.ExcuteDataSet(sql);
                                if (dsOM.Tables[0].Rows.Count > 0)
                                    OM = "NEED";
                                else
                                    OM = "NONEED";

                            string RECPLIST = "";
                                //'抓改該料號 STAGE所有製程
                                sql = " select SEQNO,RECPID from ROUTEQP t where PARTID like '" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + "%' and REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + "' order by STAGE,SEQNO ";
                                db.setConn(conn); //2024-07-08
                                dsCHK2 = db.ExcuteDataSet(sql);
                                for (int j = 0; j < dsCHK2.Tables[0].Rows.Count; j++)
                                {
                                    RECPLIST += dsCHK2.Tables[0].Rows[j]["RECPID"].ToString().Replace("7DRI2", "7DRZ") + ";"; //將7DRI2(埋鑽)5碼 變成7DRZ以利4碼判斷
                                                                                                                              //msgbox sql & "______"& RECPLIST
                                }
                                oldPART = str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1);
                                oldSTAGE = str_STAGE;
                            //'判斷料號是否為MSAP並抓取MSAP規則

                            //if (dtHist.Rows[i]["MSAP"].ToString() == "ON")
                            //{
                            //    if (mSAP == "NEED")
                            //        sql = "select * from HOLDTIME_RULE t where t.protype='mSAP' and PAREA='" + PRODAREA + "' ";
                            //    else
                            //        sql = "select * from HOLDTIME_RULE t where t.protype<>'mSAP' and PAREA='" + PRODAREA + "' ";
                            //}
                            //else
                            //{
                            //    sql = "select * from HOLDTIME_RULE t where t.protype<>'mSAP' and t.protype<>'PATTERN' and PAREA='" + PRODAREA + "' ";
                            //}
                            //2024-07-08 判斷規則
                            string str_protype = "";
                            if (mSAP == "NEED")
                            {
                                str_protype="mSAP"  ;
                            }
                            else if(OM == "NEED")
                            {
                                str_protype = "OM";
                            }
                            else
                            {
                                str_protype = "Subtractive" ;
                            }
                            sql = "select * from HOLDTIME_RULE t where t.protype = '"+ str_protype + "' and PAREA='" + PRODAREA + "' ";


                            db.setConn(conn);
                                dsRULE = db.ExcuteDataSet(sql);
                                string str_F_LOCATION, str_F_EQPTYPE, str_F_RECP, str_F_STATE, str_HOLETIME, str_T_EQPTYPE, str_T_RECP, str_T_LOCATION;
                                if (dsRULE.Tables[0].Rows.Count > 0)
                                {
                                    dtRULE = dsRULE.Tables[0];
                                    for (int r = 0; r < dtRULE.Rows.Count; r++)
                                    {
                                        RUNTIME = "";
                                        RULETIME = "";
                                        RULEMEMO = "";
                                        str_F_LOCATION = dtRULE.Rows[r]["F_LOCATION"].ToString();
                                        str_F_EQPTYPE   = dtRULE.Rows[r]["F_EQPTYPE"].ToString();
                                        str_F_RECP = dtRULE.Rows[r]["F_RECP"].ToString();
                                        str_F_STATE = dtRULE.Rows[r]["F_STATE"].ToString();
                                        str_HOLETIME = dtRULE.Rows[r]["HOLETIME"].ToString();
                                        str_T_EQPTYPE = dtRULE.Rows[r]["T_EQPTYPE"].ToString();
                                        str_T_RECP = dtRULE.Rows[r]["T_RECP"].ToString();
                                        str_T_LOCATION = dtRULE.Rows[r]["T_LOCATION"].ToString();
                                    //msgbox RECPLIST
                                    //msgbox "___起製程___" & rsRULE("F_RECP")
                                    //msgbox "___迄製程____" & rsRULE("T_RECP")

                                    //int ok = dsCHK2.Tables[0].Select("RECPID like '%" + str_F_RECP + "%'").Length;


                                    if (RECPLIST.IndexOf(str_F_RECP.Replace("7DRI2", "7DRZ")) > 0 &
                                                RECPLIST.IndexOf(str_T_RECP.Replace("7DRI2", "7DRZ")) > 0) //判斷是該料號STAGE是否有規則表起和迄站
                                        {
                                            if (str_F_STATE == "上機")
                                            {
                                                //判斷該工令,STAGE是否已跑過該規則的FROM細站
                                                sql = @"select MIN(EVTIME) as EVTIME  from dgv22.hist a,dgv22.HIST_EVET b 
                                        where a.lotid = '" + str_LOTID + @"'  and a.HISTKEY=b.HISTKEY  AND 
                                        REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') ='" + str_STAGE + @"'  
                                        and a.LOCATION='" + str_F_LOCATION + "' and a.EQPTYPE='" + str_F_EQPTYPE + @"' and
                                        SUBSTR(REPLACE(a.RECPID,'7DRI2','7DRZ'),1,4)='" + str_F_RECP.Replace("7DRI2", "7DRZ") + @"'
                                        and (b.EVTYPE IN ('NTKI')) 
                                        union 
                                        select MIN(EVTIME) as EVTIME  from dgv22.actl a,dgv22.actl_EVET b 
                                        where a.lotid = '" + str_LOTID + @"'  and a.lotid=b.lotid  AND REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') ='" + str_STAGE + @"' 
                                        and a.LOCATION='" + str_F_LOCATION + @"' and a.EQPTYPE='" + str_F_EQPTYPE + @"' 
                                        and SUBSTR(a.RECPID,1,4)='" + str_F_RECP.Replace("7DRI2", "7DRZ") + @"'
                                        and EVTYPE='NTKI'";
                                            }
                                            else if (str_F_STATE == "下機")
                                            {
                                                //判斷該工令,STAGE是否已跑過該規則的FROM細站
                                                sql = @"select MIN(EVTIME) as EVTIME  from dgv22.hist a,dgv22.HIST_EVET b 
                                            where a.lotid = '" + str_LOTID + @"'  and a.HISTKEY=b.HISTKEY  AND 
                                            REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') ='" + str_STAGE + @"'  
                                            and a.LOCATION = '" + str_F_LOCATION + "' and a.EQPTYPE = '" + str_F_EQPTYPE + @"' 
                                            and SUBSTR(REPLACE(a.RECPID,'7DRI2','7DRZ'),1,4)= '" + str_F_RECP.Replace("7DRI2", "7DRZ") + @"'
                                            and(b.EVTYPE IN('NTKO') OR(b.EVTYPE = 'CLPR' AND(b.evvariant LIKE 'M_$STATUS_C_R_C_Y' or b.evvariant LIKE 'M_$STATUS_C_W_C_Y'))) 
                                            union 
                                            select MIN(EVTIME)  from dgv22.actl a,dgv22.actl_EVET b 
                                            where a.lotid = '" + str_LOTID + @"'  and a.lotid = b.lotid  AND REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') = '" + str_STAGE + @"' 
                                            and a.LOCATION = '" + str_F_LOCATION + @"' and a.EQPTYPE = '" + str_F_EQPTYPE + @"' 
                                            and SUBSTR(a.RECPID,1,4)= '" + str_F_RECP + @"'
                                            and(b.EVTYPE IN('NTKO') OR(b.EVTYPE = 'CLPR' AND b.evvariant LIKE 'M_$STATUS_C_R_C_Y')) ";
                                            }

                                            DataSet dsHIST2 = new DataSet();
                                            DataTable dtHIST2 = new DataTable();
                                            db.setConn(conn);
                                            dsHIST2 = db.ExcuteDataSet(sql);
                                            switch (dtHist.Rows[i]["state"].ToString())
                                            {
                                                case "E":
                                                case "G":
                                                case "H":
                                                case "K":
                                                case "L":
                                                case "O":
                                                case "P":
                                                case "T":
                                                case "U":
                                                case "V":
                                                case "X":
                                                case "Y":
                                                case "a":
                                                case "c":
                                                case "f":
                                                case "g":
                                                case "h":
                                                case "j":
                                                case "k":
                                                    state = "HOLD";
                                                    break;
                                                case "D":
                                                    state = "WAIT";
                                                    break;
                                                case "I":
                                                case "J":
                                                    state = "RUN";
                                                    break;
                                                case "B":
                                                    state = "COMPLETE";
                                                    break;
                                                case "e":
                                                    state = "FINISH WAIT";
                                                    break;
                                                case "i":
                                                    state = "FINISH";
                                                    break;
                                                default:
                                                    state = "W";
                                                    break;
                                            }
                                            if (dsHIST2.Tables[0].Rows.Count == 0) //2024-07-08
                                            {
                                                RUNTIME = "";
                                                RULETIME = str_HOLETIME;
                                                RULEMEMO = "規則符合-還未到起製程";
                                                LIGHT = "1";
                                            }
                                            else
                                            {
                                                dtHIST2 = dsHIST2.Tables[0];
                                                if (dtHIST2.Rows[0]["EVTIME"] == DBNull.Value) //2024-07-08
                                                {
                                                    RUNTIME = "";
                                                    RULETIME = str_HOLETIME;
                                                    RULEMEMO = "規則符合-還未到起製程";
                                                    LIGHT = "1";
                                                }
                                                else
                                                {
                                                    //rsHIST2("EVTIME") 起站 上機或下機時間
                                                    if (dtRULE.Rows[r]["T_STATE"].ToString() == "下機")
                                                    {
                                                        //判斷該工令,STAGE是否已跑過該規則的TO細站下機台
                                                        sql = @"select MIN(EVTIME) as EVTIME from dgv22.hist a,dgv22.HIST_EVET b 
                                                    where a.lotid = '" + str_LOTID + @"'  and a.HISTKEY=b.HISTKEY  AND REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') ='" + str_STAGE + @"' 
                                                    and a.LOCATION='" + str_T_LOCATION + "' and a.EQPTYPE='" + str_T_EQPTYPE + @"' and 
                                                    SUBSTR(REPLACE(a.RECPID,'7DRI2','7DRZ'),1,4)='" + str_T_RECP.Replace("7DRI2", "7DRZ") + @"'
                                                    and (b.EVTYPE IN ('NTKO') OR (b.EVTYPE = 'CLPR' AND b.evvariant LIKE 'M_$STATUS_C_R_C_Y')) 
                                                    union 
                                                    select MIN(EVTIME)  from dgv22.actl a,dgv22.actl_EVET b 
                                                    where a.lotid = '" + str_LOTID + @"'  and a.lotid=b.lotid  AND REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') ='" + str_STAGE + @"' 
                                                    and a.LOCATION='" + str_T_LOCATION + @"' and a.EQPTYPE='" + str_T_EQPTYPE + @"' and 
                                                    SUBSTR(REPLACE(a.RECPID,'7DRI2','7DRZ'),1,4)='" + str_T_RECP.Replace("7DRI2", "7DRZ") + @"'
                                                    and (b.EVTYPE IN ('NTKO') OR (b.EVTYPE = 'CLPR' AND b.evvariant LIKE 'M_$STATUS_C_R_C_Y')) ";
                                                    }
                                                    else if (dtRULE.Rows[r]["T_STATE"].ToString() == "上機")
                                                    {
                                                        //判斷該工令,STAGE是否已跑過該規則的TO細站上機台
                                                        sql = @"select MIN(EVTIME) as EVTIME from dgv22.hist a,dgv22.HIST_EVET b 
                                                    where a.lotid = '" + str_LOTID + @"'  and a.HISTKEY=b.HISTKEY  AND REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') ='" + str_STAGE + @"' 
                                                    and a.LOCATION='" + dtRULE.Rows[r]["T_LOCATION"] + "' and a.EQPTYPE='" + str_T_EQPTYPE + @"' and 
                                                    SUBSTR(REPLACE(a.RECPID,'7DRI2','7DRZ'),1,4)='" + str_T_RECP.Replace("7DRI2", "7DRZ") + @"'
                                                    and EVTYPE='NTKI'
                                                    union 
                                                    select MIN(EVTIME) as EVTIME  from dgv22.actl a,dgv22.actl_EVET b 
                                                    where a.lotid = '" + str_LOTID + @"'  and a.lotid=b.lotid  AND REPLACE(REPLACE(a.STAGE,'ST1','OU1'),'SU1','OU1') ='" + str_STAGE + @"' 
                                                    and a.LOCATION='" + str_T_LOCATION + @"' and a.EQPTYPE='" + str_T_EQPTYPE + @"' and 
                                                    SUBSTR(REPLACE(a.RECPID,'7DRI2','7DRZ'),1,4)='" + str_T_RECP.Replace("7DRI2", "7DRZ") + @"'
                                                    and EVTYPE='NTKI'";
                                                    }

                                                    DataSet dsHIST3 = new DataSet();
                                                    db.setConn(conn);
                                                    dsHIST3 = db.ExcuteDataSet(sql);
                                                    //迄站 上機或下機時間	
                                                    if (dsHIST3.Tables[0].Rows.Count > 0 && dsHIST3.Tables[0].Rows[0]["EVTIME"] != DBNull.Value) //2024-07-08
                                                    {
                                                        RUNTIME = (Math.Round(((new TimeSpan(DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks - DateTime.Parse(dsHIST3.Tables[0].Rows[0]["EVTIME"].ToString()).Ticks).TotalMinutes) / 60), 2)).ToString();
                                                        //RUNTIME = ROUND(DateDiff("n", CDate(rsHIST2("EVTIME")), CDate(rsHIST3("EVTIME"))) / 60, 2)
                                                        RULETIME = str_HOLETIME;
                                                        if (decimal.Parse(RUNTIME) > decimal.Parse(RULETIME))
                                                        {
                                                            if (dtRULE.Rows[r]["T_STATE"].ToString() == "上機")
                                                            {
                                                                RULEMEMO = "規則不符合-已在迄製程上過機台";
                                                            }
                                                            else if (dtRULE.Rows[r]["T_STATE"].ToString() == "下機")
                                                            {
                                                                RULEMEMO = "規則不符合-已在迄製程下過機台";
                                                            }
                                                            LIGHT = "3";
                                                            R_stage = "";
                                                            if (dtRULE.Rows[r]["LOCKRECP1"] != DBNull.Value)
                                                            {
                                                                if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                    dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                {
                                                                    R_stage = "ST1";
                                                                }
                                                                else
                                                                {
                                                                    R_stage = str_STAGE;
                                                                }
                                                                sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP1"].ToString() + "'";
                                                                DataSet dsHIST4 = new DataSet();
                                                                db.setConn(conn);
                                                                dsHIST4 = db.ExcuteDataSet(sql);
                                                                if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                {
                                                                    //mark
                                                                    //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                    //        RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP1"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE1"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC1"].ToString());
                                                                }
                                                                else
                                                                {
                                                                //    sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP1"].ToString() + "'";
                                                                //    db.setConn(conn);
                                                                //    rtn = db.ExcuteNonQuery(sql);
                                                                }
                                                            }
                                                            if (dtRULE.Rows[r]["LOCKRECP2"] != DBNull.Value)
                                                            {
                                                                if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                    dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                {
                                                                    R_stage = "ST1";
                                                                }
                                                                else
                                                                {
                                                                    R_stage = str_STAGE;
                                                                }
                                                                sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP2"].ToString() + "'";
                                                                DataSet dsHIST4 = new DataSet();
                                                                db.setConn(conn);
                                                                dsHIST4 = db.ExcuteDataSet(sql);
                                                                if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                {
                                                                    //mark
                                                                    //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                    //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP2"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE2"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC2"].ToString());
                                                                }
                                                                else
                                                                {
                                                                //mark
                                                                //    sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP2"].ToString() + "'";
                                                                //    db.setConn(conn);
                                                                //    rtn = db.ExcuteNonQuery(sql);
                                                                }
                                                            }
                                                            if (dtRULE.Rows[r]["LOCKRECP3"] != DBNull.Value)
                                                            {
                                                                if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                    dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                {
                                                                    R_stage = "ST1";
                                                                }
                                                                else
                                                                {
                                                                    R_stage = str_STAGE;
                                                                }
                                                                sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP3"].ToString() + "'";
                                                                DataSet dsHIST4 = new DataSet();
                                                                db.setConn(conn);
                                                                dsHIST4 = db.ExcuteDataSet(sql);
                                                                if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                {
                                                                    //mark
                                                                    //db.setConn(conn);
                                                                    //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(), state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                    //RUNTIME, RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP3"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE3"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC3"].ToString());
                                                                }
                                                                else
                                                                {
                                                                //mark
                                                                //    sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP3"].ToString() + "'";
                                                                //    db.setConn(conn);
                                                                //    rtn = db.ExcuteNonQuery(sql);
                                                                }
                                                            }
                                                            if (dtRULE.Rows[r]["LOCKRECP4"] != DBNull.Value)
                                                            {
                                                                if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                    dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                {
                                                                    R_stage = "ST1";
                                                                }
                                                                else
                                                                {
                                                                    R_stage = str_STAGE;
                                                                }
                                                                sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP4"].ToString() + "'";
                                                                DataSet dsHIST4 = new DataSet();
                                                                db.setConn(conn);
                                                                dsHIST4 = db.ExcuteDataSet(sql);
                                                                if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                {
                                                                    //mark
                                                                    //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                    //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP4"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE4"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC4"].ToString());
                                                                }
                                                                else
                                                                {
                                                                //mark
                                                                //    sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP4"].ToString() + "'";
                                                                //    db.setConn(conn);
                                                                //    rtn = db.ExcuteNonQuery(sql);
                                                                }
                                                            }
                                                            if (dtRULE.Rows[r]["LOCKRECP1"] == DBNull.Value && dtRULE.Rows[r]["LOCKRECP2"] == DBNull.Value &&
                                                                dtRULE.Rows[r]["LOCKRECP3"] == DBNull.Value && dtRULE.Rows[r]["LOCKRECP4"] == DBNull.Value)
                                                            {
                                                                R_stage = str_STAGE;
                                                                sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and 
                                                            RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + "' and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP is null ";
                                                                DataSet dsHIST4 = new DataSet();
                                                                db.setConn(conn);
                                                                dsHIST4 = db.ExcuteDataSet(sql);
                                                                if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                {
                                                                //mark
                                                                    //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                    //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", "", "", "");
                                                                }
                                                                else
                                                                {
                                                                //mark
                                                                 //   sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"' ,CHKDATE=sysdate
                                                                 //where LOTID= '" + str_LOTID + @"' and PARTID= '" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                 //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                 //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + "' ";
                                                                 //   db.setConn(conn);
                                                                 //   rtn = db.ExcuteNonQuery(sql);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (dtRULE.Rows[r]["T_STATE"].ToString() == "上機")
                                                            {
                                                                RULEMEMO = "規則符合-已在迄製程上過機台";
                                                            }
                                                            else if (dtRULE.Rows[r]["T_STATE"].ToString() == "下機")
                                                            {
                                                                RULEMEMO = "規則符合-已在迄製程下過機台";
                                                            }
                                                            LIGHT = "1";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //判斷該工令,STAGE若未到規則TO細站則算時間 2024-07-08
                                                        if ((new TimeSpan(DateTime.Now.Ticks-DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks  ).TotalHours) > int.Parse(dtRULE.Rows[r]["ALERTTIME"].ToString()) ||
                                                            (new TimeSpan(DateTime.Now.Ticks-DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks  ).TotalHours) > int.Parse(str_HOLETIME))
                                                        {
                                                            BODY.Append("<tr>");
                                                            BODY.Append(" < td><a href='http://10.14.65.71/HistRecordRpt_2_NEW.asp?lotid=" + str_LOTID + "'>" + str_LOTID + "</a>");
                                                            BODY.Append("< a href='http://10.14.65.71/mes/CusMenu/LocWip/holdtime_Q2.asp??Groupid=" + str_LOTID.Substring(0, 1) + "&LOTID=" + str_LOTID + "'>.</a> ");
                                                            BODY.Append("</ td >");
                                                            BODY.Append("< td >< a href = 'http://10.14.65.71/PROMISSTEP.asp?RunForm=" + RUNFORM + "&RunCardNo=" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + "' > " + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + " </ a ></ td > ");
                                                            BODY.Append("< td > " + dtHist.Rows[i]["CR2"].ToString() + " </ td >");
                                                            BODY.Append("< td > " + str_STAGE + " </ td >< td > " + dtHist.Rows[i]["LOCATION"].ToString() + " </ td >< td > " + dtHist.Rows[i]["RECPID"].ToString() + " </ td >");
                                                            BODY.Append("< td > " + state + " </ td >< td > " + dtRULE.Rows[r]["RULENAME"].ToString() + " </ td >< td > " + str_F_RECP + " </ td >< td > " + str_T_RECP + " </ td >");
                                                            //2024-07-08
                                                            if ((new TimeSpan(DateTime.Now.Ticks-DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks  ).TotalHours) > int.Parse(str_HOLETIME))
                                                            {
                                                                BODY.Append("<td>");
                                                                //2024-07-08
                                                                BODY.Append((new TimeSpan(DateTime.Now.Ticks - DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks  ).TotalHours));
                                                                BODY.Append("</td><td>" + dtRULE.Rows[r]["ALERTTIME"].ToString() + "</td><td>");
                                                                BODY.Append(str_HOLETIME);
                                                                BODY.Append("</td><td>規則HOLD-已超過QUEUE時間未到迄製程</td><td bgcolor='red'>3</td><td>");
                                                                BODY.Append(dtHist.Rows[i]["CURMAINQTY"].ToString() + "</td><td>" + dtHist.Rows[i]["MAINMATTYPE"].ToString() + "</td></tr>");
                                                                //2024-07-08
                                                                RUNTIME = (new TimeSpan(DateTime.Now.Ticks-DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks  ).TotalHours).ToString();
                                                                RULETIME = str_HOLETIME;
                                                                RULEMEMO = "規則HOLD-已超過QUEUE時間未到迄製程";
                                                                LIGHT = "3";
                                                                R_stage = "";
                                                                if (dtRULE.Rows[r]["LOCKRECP1"] != DBNull.Value)
                                                                {
                                                                    if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                   dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                    {
                                                                        R_stage = "ST1";
                                                                    }
                                                                    else
                                                                    {
                                                                        R_stage = str_STAGE;
                                                                    }
                                                                    sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP2"].ToString() + "'";
                                                                    DataSet dsHIST4 = new DataSet();
                                                                    db.setConn(conn);
                                                                    dsHIST4 = db.ExcuteDataSet(sql);
                                                                    if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                    {
                                                                        //mark
                                                                        //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                        //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP2"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE2"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC2"].ToString());
                                                                    }
                                                                    else
                                                                    {
                                                                    //mark
                                                                //        sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP2"].ToString() + "'";
                                                                //        db.setConn(conn);
                                                                //        rtn = db.ExcuteNonQuery(sql);
                                                                    }


                                                                }
                                                                if (dtRULE.Rows[r]["LOCKRECP2"] != DBNull.Value)
                                                                {
                                                                    if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                   dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                    {
                                                                        R_stage = "ST1";
                                                                    }
                                                                    else
                                                                    {
                                                                        R_stage = str_STAGE;
                                                                    }
                                                                    sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP2"].ToString() + "'";
                                                                    DataSet dsHIST4 = new DataSet();
                                                                    db.setConn(conn);
                                                                    dsHIST4 = db.ExcuteDataSet(sql);
                                                                    if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                    {
                                                                        //mark
                                                                        //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                        //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP2"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE2"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC2"].ToString());
                                                                    }
                                                                    else
                                                                    {
                                                                    //mark
                                                                //        sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP2"].ToString() + "'";
                                                                //        db.setConn(conn);
                                                                //        rtn = db.ExcuteNonQuery(sql);
                                                                    }


                                                                }
                                                                if (dtRULE.Rows[r]["LOCKRECP3"] != DBNull.Value)
                                                                {
                                                                    if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                        dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                    {
                                                                        R_stage = "ST1";
                                                                    }
                                                                    else
                                                                    {
                                                                        R_stage = str_STAGE;
                                                                    }
                                                                    sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP3"].ToString() + "'";
                                                                    DataSet dsHIST4 = new DataSet();
                                                                    db.setConn(conn);
                                                                    dsHIST4 = db.ExcuteDataSet(sql);
                                                                    if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                    {
                                                                        //mark
                                                                        //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                        //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP3"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE3"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC3"].ToString());
                                                                    }
                                                                    else
                                                                    {
                                                                    //mark
                                                                //        sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP3"].ToString() + "'";
                                                                //        db.setConn(conn);
                                                                //        rtn = db.ExcuteNonQuery(sql);
                                                                    }
                                                                }
                                                                if (dtRULE.Rows[r]["LOCKRECP4"] != DBNull.Value)
                                                                {
                                                                    if (dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("TAU") > 0 ||
                                                                        dtRULE.Rows[r]["LOCKRECP1"].ToString().IndexOf("FQC") > 0)
                                                                    {
                                                                        R_stage = "ST1";
                                                                    }
                                                                    else
                                                                    {
                                                                        R_stage = str_STAGE;
                                                                    }
                                                                    sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' 
                                                            and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP4"].ToString() + "'";
                                                                    DataSet dsHIST4 = new DataSet();
                                                                    db.setConn(conn);
                                                                    dsHIST4 = db.ExcuteDataSet(sql);
                                                                    if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                    {
                                                                    //mark
                                                                        //rtn = INSERT_HOLDTIME_HOLD( str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                        //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", dtRULE.Rows[r]["LOCKRECP4"].ToString(), dtRULE.Rows[r]["LOCKRECP_TYPE4"].ToString(), dtRULE.Rows[r]["LOCKRECP_NOTIC4"].ToString());
                                                                    }
                                                                    else
                                                                    {
                                                                    //mark
                                                                //        sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"',CHKDATE=sysdate 
                                                                //where  LOTID='" + str_LOTID + @"' and PARTID='" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' 
                                                                //and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"' and LOCKRECP='" + dtRULE.Rows[r]["LOCKRECP4"].ToString() + "'";
                                                                //        db.setConn(conn);
                                                                //        rtn = db.ExcuteNonQuery(sql);
                                                                    }
                                                                }
                                                                if (dtRULE.Rows[r]["LOCKRECP1"] == DBNull.Value && dtRULE.Rows[r]["LOCKRECP2"] == DBNull.Value &&
                                                                dtRULE.Rows[r]["LOCKRECP3"] == DBNull.Value && dtRULE.Rows[r]["LOCKRECP4"] == DBNull.Value)
                                                                {
                                                                    R_stage = str_STAGE;
                                                                    sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"'  and 
                                                            RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + "' and PAREA='" + PRODAREA + "' and LIGHTS='3' and LOCKRECP is null ";
                                                                    DataSet dsHIST4 = new DataSet();
                                                                    db.setConn(conn);
                                                                    dsHIST4 = db.ExcuteDataSet(sql);
                                                                    if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                    {
                                                                        //mark
                                                                        //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), R_stage, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                        //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "3", "", "", "");
                                                                    }
                                                                    else
                                                                    {
                                                                    //mark
                                                                 //       sql = @"UPDATE HOLDTIME_HOLD set RUNTIME='" + RUNTIME + @"' ,CHKDATE=sysdate
                                                                 //where LOTID= '" + str_LOTID + @"' and PARTID= '" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"' 
                                                                 //and  REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' and N_RECP='" + dtHist.Rows[i]["RECPID"].ToString() + @"'
                                                                 //and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + "' ";
                                                                 //       db.setConn(conn);
                                                                 //       rtn = db.ExcuteNonQuery(sql);
                                                                    }
                                                                }
                                                                if (dtRULE.Rows[r]["MAILADR"].ToString() != "")
                                                                {
                                                                    string[] mailary = dtRULE.Rows[r]["MAILADR"].ToString().Split(';');
                                                                    string mailadress = "";
                                                                    foreach (var item in mailary)
                                                                    {
                                                                        if (mailadress.IndexOf(item) < 1)
                                                                        {
                                                                            if (item.IndexOf("@") > 0)
                                                                            {
                                                                                aTo += item + ";";
                                                                                mailadress = mailadress + item + ";";
                                                                            }
                                                                            else
                                                                            {
                                                                               sql = "select EMAIL from eeqp.hr_emp_base@run.world where ID='" + item + "'";
                                                                                DataSet dsMail = new DataSet();
                                                                                db.setConn(conn);
                                                                                dsMail = db.ExcuteDataSet(sql);
                                                                                if (dsMail.Tables[0].Rows.Count>0)
                                                                                {
                                                                                    aTo += dsMail.Tables[0].Rows[0]["EMAIL"].ToString() + ";";
                                                                                    mailadress = mailadress + dsMail.Tables[0].Rows[0]["EMAIL"].ToString() + ";";
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            //2024-07-08
                                                            else if ((new TimeSpan(DateTime.Now.Ticks-DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks ).TotalHours) > int.Parse(dtRULE.Rows[r]["ALERTTIME"].ToString()))
                                                            {
                                                            //2024-07-08
                                                             BODY.Append("<td>" + (new TimeSpan(DateTime.Now.Ticks-DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks ).TotalHours) + "</td><td>" + dtRULE.Rows[r]["ALERTTIME"].ToString() + "</td><td>" + str_HOLETIME + "</td><td>規則HOLD-已超過ALERT時間未到迄製程</td><td bgcolor='yellow'>2</td><td>" + dtHist.Rows[i]["CURMAINQTY"].ToString() + "</td><td>" + dtHist.Rows[i]["MAINMATTYPE"].ToString() + "</td></tr>");
                                                            //2024-07-08
                                                            RUNTIME = (new TimeSpan(DateTime.Now.Ticks-DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks).TotalHours).ToString();
                                                                RULETIME = str_HOLETIME;
                                                                RULEMEMO = "規則ALERT-已超過ALERT時間未到迄製程";
                                                                LIGHT = "2";
                                                                sql = @" select LOTID from HOLDTIME_HOLD where LOTID='" + str_LOTID + @"' and 
                                                            REPLACE(REPLACE(STAGE,'ST1','OU1'),'SU1','OU1')='" + str_STAGE + @"' and 
                                                            N_LOCATION='" + dtHist.Rows[i]["LOCATION"].ToString() + @"' and 
                                                            RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + "' and PAREA='" + PRODAREA + "' and LIGHTS='2'";
                                                                DataSet dsHIST4 = new DataSet();
                                                                db.setConn(conn);
                                                                dsHIST4 = db.ExcuteDataSet(sql);
                                                                if (dsHIST4.Tables[0].Rows.Count == 0)
                                                                {
                                                                    //mark
                                                                    //rtn = INSERT_HOLDTIME_HOLD(str_LOTID, str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1), str_STAGE, dtHist.Rows[i]["LOCATION"].ToString(), dtHist.Rows[i]["RECPID"].ToString(),  state, dtRULE.Rows[r]["ITEMNO"].ToString(),
                                                                    //RUNTIME,  RULETIME,  RULEMEMO,  PRODAREA, "2", "", "", "");
                                                                }
                                                                if (dtRULE.Rows[r]["MAILADR"].ToString() != "")
                                                                {
                                                                    string[] mailary = dtRULE.Rows[r]["MAILADR"].ToString().Split(';');
                                                                    string mailadress = "";
                                                                    foreach (var item in mailary)
                                                                    {
                                                                        if (mailadress.IndexOf(item) < 1)
                                                                        {
                                                                            if (item.IndexOf("@") > 0)
                                                                            {
                                                                                aTo += item + ";";
                                                                                mailadress = mailadress + item + ";";
                                                                            }
                                                                            else
                                                                            {
                                                                              sql = "select EMAIL from eeqp.hr_emp_base@run.world where ID='" + item + "'";
                                                                                DataSet dsMail = new DataSet();
                                                                                db.setConn(conn);
                                                                                dsMail = db.ExcuteDataSet(sql);
                                                                                if (dsMail.Tables[0].Rows.Count>0)
                                                                                {
                                                                                    aTo += dsMail.Tables[0].Rows[0]["EMAIL"].ToString() + ";";
                                                                                    mailadress = mailadress + dsMail.Tables[0].Rows[0]["EMAIL"].ToString() + ";";
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                RUNTIME = (new TimeSpan(DateTime.Parse(dtHIST2.Rows[0]["EVTIME"].ToString()).Ticks - DateTime.Now.Ticks).TotalHours).ToString();
                                                                RULETIME = str_HOLETIME;
                                                                RULEMEMO = "規則符合-未到迄製程不過在控管時間內";
                                                                LIGHT = "1";
                                                            }

                                                        }


                                                    }


                                                }

                                            }

                                        }
                                        else
                                        {
                                            RUNTIME = "";
                                            RULETIME = "";
                                            RULEMEMO = "規則符合-該料號STAGE無此製程";
                                            LIGHT = "1";
                                        }

                                        if ((RULEMEMO == "規則符合-已在迄製程下過機台") || (RULEMEMO == "規則符合-已在迄製程上過機台") || (RULEMEMO == "規則不符合-已在迄製程下過機台")
                                            || (RULEMEMO == "規則不符合-已在迄製程上過機台"))
                                        {
                                            LIGHT = "1";
                                            sql = @"select * from HOLDTIME_CHKLOG where PAREA='" + PRODAREA + "' and LOTID='" + str_LOTID + @"'  
                                        and STAGE='" + str_STAGE + "' and RULE_ITEMNO='" + dtRULE.Rows[r]["ITEMNO"].ToString() + "' and RULEMEMO='" + RULEMEMO + "' ";
                                            //DataSet dsCHK9= new DataSet();
                                            db.setConn(conn);
                                            dsCHK = db.ExcuteDataSet(sql);
                                            if (dsCHK.Tables[0].Rows.Count == 0)
                                            {
                                            //mark
                                            //    sql = @"INSERT into HOLDTIME_CHKLOG(LOTID,PARTID,STAGE,N_LOCATION,N_RECP,N_STATE,RULE_ITEMNO,RUNTIME,RULETIME,RULEMEMO,CHKDATE,LIGHTS,PAREA,CR2,CURMAINQTY,MAINMATTYPE)
                                            //values('" + str_LOTID + @"','" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"',
                                            //'" + str_STAGE + @"','" + dtHist.Rows[i]["LOCATION"].ToString() + @"','" + dtHist.Rows[i]["RECPID"].ToString() + @"','" + state + @"' 
                                            //,'" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"','" + RUNTIME + @"','" + RULETIME + @"','" + RULEMEMO + @"',sysdate,'" + LIGHT + @"','" + PRODAREA + @"',
                                            //    '" + dtHist.Rows[i]["CR2"].ToString() + "','" + dtHist.Rows[i]["CURMAINQTY"].ToString() + "','" + dtHist.Rows[i]["MAINMATTYPE"].ToString()  +"') ";
                                            //    db.setConn(conn);
                                            //    rtn = db.ExcuteNonQuery(sql);
                                            }
                                            else //20191121有重覆上下機的
                                            {

                                            }
                                        }
                                        else if (RULEMEMO == "規則符合-該料號STAGE無此製程")
                                        {

                                        }
                                        else
                                        {
                                            LIGHT = "";
                                            //mark
                                            //sql = @"INSERT into HOLDTIME_CHKLOG(LOTID,PARTID,STAGE,N_LOCATION,N_RECP,N_STATE,RULE_ITEMNO,RUNTIME,RULETIME,RULEMEMO,CHKDATE,LIGHTS,PAREA,CR2,CURMAINQTY,MAINMATTYPE) 
                                            //values('" + str_LOTID + @"','" + str_PARTID.Substring(0, str_PARTID.IndexOf("-") - 1) + @"',
                                            //'" + str_STAGE + @"','" + dtHist.Rows[i]["LOCATION"].ToString() + @"','" + dtHist.Rows[i]["RECPID"].ToString() + @"','" + state + @"' 
                                            //,'" + dtRULE.Rows[r]["ITEMNO"].ToString() + @"','" + RUNTIME + @"','" + RULETIME + @"','" + RULEMEMO + @"',sysdate,'" + LIGHT + @"','" + PRODAREA + @"',
                                            //'" + dtHist.Rows[i]["CR2"].ToString() + "','" + dtHist.Rows[i]["CURMAINQTY"].ToString() + "','" + dtHist.Rows[i]["MAINMATTYPE"].ToString() + "') ";
                                            //db.setConn(conn);
                                            //rtn = db.ExcuteNonQuery(sql);
                                        }

                                    }
                                } //dsRule

                            }

                        //}
                    }
                }
                BODY.Append("</table></body>");
                //2019/04/26 山鶯開始鎖帳
                //mark
                //sql = @"update HOLDTIME_HOLD t set t.islock='Y' where t.parea='" + PRODAREA + @"' and t.chkdate>sysdate-1 and t.lights='3'  " +
                //    "   and t.lockrecp is not null  ";
                //db.setConn(conn);
                //rtn = db.ExcuteNonQuery(sql);
                //20210209刪除1.5前資料 mark
                //sql = " delete from HOLDTIME_CHKLOG where CHKDATE<sysdate-550 ";
                //db.setConn(conn);
                //rtn = db.ExcuteNonQuery(sql);
                //20210524增加不知為何都沒黃燈重新判斷 mark
                //sql = " update HOLDTIME_CHKLOG t  set LIGHTS='2' where t.parea='" + PRODAREA + "' and t.chkdate>sysdate-30 and RUNTIME>(RULETIME*0.8) and RUNTIME<(RULETIME) and LIGHTS='1' ";
                //db.setConn(conn);
                //rtn = db.ExcuteNonQuery(sql);

                aTo += "edward_lee@unimicron.com";
                aTo += "ting@unimicron.com"; //林昌鼎
                aTo += "chingchi_lee@unimicron.com"; //李慶基	
                aTo += "asli_chen@unimicron.com"; //陳靜雯
                aTo += "james_chiu@unimicron.com"; //James Chiu (邱俊豪)
                aTo += "sarachang@unimicron.com"; //張淑媛
                aTo += "iris_lai@unimicron.com"; //賴靖玟
                aTo += "09744@unimicron.com"; //陳世國
                aTo += "vincent_luo@unimicron.com"; //羅文深
                aTo += "Jack_Jang@unimicron.com"; //Jack Jang (張鈞傑)
                aTo += "Jenq_Cheng@unimicron.com"; //鄭淨文
                aTo += "Joe_Hsu@unimicron.com"; //徐世威 
                aTo += "John_Su@unimicron.com"; //蘇祈恩

                wsSendMail.WebServiceSoapClient ws = new wsSendMail.WebServiceSoapClient();
                ws.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(30); //設定超時時間為30秒
                int retyrCount = 3;
                int retyrDelayMilliseconds = 1000;//重試延遲間為1秒
                for (int t = 0; t < retyrCount; t++)
                {
                    try
                    {
                        //Boolean sendResult = ws.Mail_Relay("bga_cim@unimicron.com", "UNIMICRON-ICT", aTo, aCc, bCc, aSubject, true, BODY.ToString(), "");
                        break;//如果成功就退出
                    }
                    catch
                    {
                        //處理錯誤，並等待一段時間後進行重試
                        System.Threading.Thread.Sleep(retyrDelayMilliseconds);
                    }
                    //if (!sendResult) { throw new Exception("寄信異常"); }
                }
                string logFilePath = "log.txt";
                Logger logger = new Logger(logFilePath);
                logger.Log(" HOLDTIME結束 : Success.");

            }
            catch (Exception ex)
            {
                string logFilePath = "log.txt";
                Logger logger = new Logger(logFilePath);
                logger.Log(" HOLDTIME結束  :"+ ex.Message);
            }



        }


        public int INSERT_HOLDTIME_HOLD(string LOTID,string PARTID,string stage,string LOCATION,string RECPID,string state,string ITEMNO,
            string RUNTIME,string RULETIME,string RULEMEMO,string PRODAREA,string LIGHTS, string LOCKRECP,string LOCKRECP_TYPE,string LOCKRECP_NOTIC)
        {
            OleDB db = new OleDB();
            string sql = @"INSERT into HOLDTIME_HOLD(LOTID,PARTID,STAGE,N_LOCATION,N_RECP,N_STATE,RULE_ITEMNO,RUNTIME,
                                                                RULETIME,RULEMEMO,CHKDATE,PAREA,LIGHTS,LOCKRECP,LOCKRECP_TYPE,LOCKRECP_NOTIC) 
                                                                values('" + LOTID + @"','" + (PARTID.IndexOf("-") - 1) + @"',
                                                                '" + stage + @"','" + LOCATION + @"','" + RECPID + @"','" + state + @"' 
                                                                 ,'" + ITEMNO + @"','" + RUNTIME + @"','" + RULETIME + @"','" + RULEMEMO + @"',sysdate,'" + PRODAREA + @"','"+ LIGHTS + @"',
                                                                 '" + LOCKRECP + "','" + LOCKRECP_TYPE + "','" + LOCKRECP_NOTIC + "') ";
            db.setConn(conn);
            return db.ExcuteNonQuery(sql);
        }

        public void checkHoldTime()
        {
            List<FACT> myFACT = new List<FACT>();
            myFACT.Add(new FACT
            {
                PRODAREA = "PCB3",
                PRODAREA_NAME = "蘆二廠",
                Groupid = 3
            });
            myFACT.Add(new FACT
            {
                PRODAREA = "PCB5",
                PRODAREA_NAME = "蘆三廠",
                Groupid = 5
            });
            myFACT.Add(new FACT
            {
                PRODAREA = "PCB6",
                PRODAREA_NAME = "合江廠",
                Groupid = 6
            });
            myFACT.Add(new FACT
            {
                PRODAREA = "PCB8",
                PRODAREA_NAME = "興邦廠",
                Groupid = 8
            });

        }

        public class FACT
        {
            public string PRODAREA { get; set; }
            public string PRODAREA_NAME { get; set; }
            public int Groupid { get; set; }
        }

        public class Logger
        {
            private string logFilePath;
            public Logger(string filePath)
            {
                logFilePath = filePath;
            }
            public void Log(string message)
            {
                string logMessage = $"{DateTime.Now}: {message}";
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
        }

    }

   
}
