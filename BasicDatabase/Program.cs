﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using net_x;

namespace BasicDatabase
{
    class Program
    {
        public class UserInfo
        {
            public string strNick = String.Empty;
            public int iLevel = 0;
        }

        static void Main(string[] args)
        {
            string strServer = "192.168.0.22";
            string strUserId = "root";
            string strPwd = "aktlakfh!@34";
            string strDatabase = "JhRace";

            List<UserInfo> aUserInfo = new List<UserInfo>();

            CMySql mySql = new CMySql(strServer, strUserId, strPwd, strDatabase);

            try
            {
                mySql.Connect();

                CProcedure testProcedure = new CProcedure("sp_Test1", "IN _nAccountSn BIGINT", 2);
                mySql.CallProcedure<UserInfo>(testProcedure, aUserInfo);

                if (aUserInfo.Count == 0)
                    Console.WriteLine("데이터 없음");
                else
                {
                    foreach (UserInfo userInfo in aUserInfo)
                    {
                        Console.WriteLine("{0}  {1}", userInfo.strNick, userInfo.iLevel);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스에 접속할 수 없습니다.");

                Console.WriteLine(ex.ToString());
            }
        }
    }
}
