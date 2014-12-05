using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using net_x;

namespace BasicDatabase
{
//    class Program
//    {
//        public class UserInfo
//        {
//            public string strNick = String.Empty;
//            public int iLevel = 0;
//        }

//        static void Main(string[] args)
//        {
//            string strServer = "192.168.0.21";
//            string strUserId = "root";
//            string strPwd = "aktlakfh!@34";
//            string strDatabase = "JhRace";

//            List<UserInfo> aUserInfo = new List<UserInfo>();

//            CMySql mySql = new CMySql(strServer, strUserId, strPwd, strDatabase);

//            // SELECT 예제
//            try
//            {
//                mySql.Connect();

//#if PROCDEURE
//                CProcedure testProcedure = new CProcedure("sp_Select", "IN _nAccountSn BIGINT", 2);
//                mySql.CallProcedure<UserInfo>(testProcedure, aUserInfo);
//#else
//                CProcedure testQuery = new CProcedure("SELECT szNick,iLevel FROM JxAccount WHERE pkAccountSn =2");
//                mySql.CallQuery<UserInfo>(testQuery, aUserInfo);
//#endif

//                if (aUserInfo.Count == 0)
//                    Console.WriteLine("데이터 없음");
//                else
//                {
//                    foreach (UserInfo userInfo in aUserInfo)
//                    {
//                        Console.WriteLine("{0}  {1}", userInfo.strNick, userInfo.iLevel);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("데이터베이스에 접속할 수 없습니다.");

//                Console.WriteLine(ex.ToString());
//            }

//            mySql.Disconnect();
//        }
//    }

    class Program
    {
        public class UserInfo
        {
            public string strNick = String.Empty;
            public int iLevel = 0;
        }

        static void Main(string[] args)
        {
            string strServer = "192.168.0.21";
            string strUserId = "root";
            string strPwd = "aktlakfh!@34";
            string strDatabase = "JhRace";

            List<UserInfo> aUserInfo = new List<UserInfo>();

            CMySql mySql = new CMySql(strServer, strUserId, strPwd, strDatabase);

            // UPDATE 예제
            try
            {
                mySql.Connect();

#if PROCDEURE
                CProcedure testProcedure = new CProcedure("sp_Update", "IN _nAccountSn BIGINT, IN _szAccountNick VARCHAR(45)", 2, "newAccount");
                mySql.CallProcedure(testProcedure);
#else
                CProcedure testQuery = new CProcedure("UPDATE JxAccount SET szNick = 'newAccount' WHERE pkAccountSn =2");
                mySql.CallQuery<UserInfo>(testQuery, aUserInfo);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("데이터베이스에 접속할 수 없습니다.");

                Console.WriteLine(ex.ToString());
            }

            mySql.Disconnect();
        }
    }
}
