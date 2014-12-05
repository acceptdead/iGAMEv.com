using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using net_x;
using net_x.reactor;

namespace BasicClient
{
    public class JxConfig
    {
        public static readonly int JXSESSION_BUFFER_SIZE = 512;                // 세션 버퍼 사이즈
        public static readonly int JXSESSION_CONNECTED_COUNT = 1000;           // 최대 동시 접속자 수
    }

    public enum EJxSessionProtocol : ushort
    {
        JXPTC_CS_REQ_CHAT_MSG = 1,      // 채팅 요청
        JXPTC_SC_ANS_CHAT_MSG = 2,      // 채팅 응답
        JXPTC_SC_NFY_CHAT_MSG = 3,      // 채팅 통지
    }

    // 구조를 편하게 하기 위해서 프로토콜 네이밍을 활용하여 클래스 네임을 정의한다.

    // 채팅 요청 패킷
    public class _JXPTC_CS_REQ_CHAT_MSG
    {
        public string strId = String.Empty;           // 아이디
        public string strMsg = String.Empty;          // 메시지
    }

    // 채팅 응답 패킷
    public class _JXPTC_SC_ANS_CHAT_MSG
    {
        public string strId = String.Empty;           // 아이디
        public string strMsg = String.Empty;          // 메시지
    }

    // 채팅 통지
    public class _JXPTC_SC_NFY_CHAT_MSG
    {
        public string strId = String.Empty;           // 아이디
        public string strMsg = String.Empty;          // 메시지
    }

    public class CJxSession : CNetSession
    {
        // 버퍼 사이즈와 버퍼 카운트를 설정해서 베이스 클래스에 넘긴다.
        public CJxSession(int _iBufferSize, int _iBufferCount = 2)
            : base(_iBufferSize, _iBufferCount)
        {
        }

        // 메시지 전송
        public void SendReqChatMsg(string _strId, string _strMsg)
        {
            _JXPTC_CS_REQ_CHAT_MSG writeData = new _JXPTC_CS_REQ_CHAT_MSG();
            writeData.strId = _strId;
            writeData.strMsg = _strMsg;
            COutPacket.WRITE_PACKET((ushort)EJxSessionProtocol.JXPTC_CS_REQ_CHAT_MSG, writeData, this.GetWriteQueue());
            this.SendPacket();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        // 들어온 패킷을 처리한다.
        ////////////////////////////////////////////////////////////////////////////////////////////
        public override bool IncomingPacketProcess(CInPacket _IncomingPacket)
        {
            // 프로토콜
            EJxSessionProtocol eProtocol = (EJxSessionProtocol)_IncomingPacket.GetProtocol();

            switch (eProtocol)
            {
                // 채팅 메시지를 처리한다.
                case EJxSessionProtocol.JXPTC_SC_ANS_CHAT_MSG:
                    {
                        // 패킷을 파싱한다.
                        _JXPTC_SC_ANS_CHAT_MSG readData = CInPacket.READ_PACKET<_JXPTC_SC_ANS_CHAT_MSG>(_IncomingPacket);

                        Console.WriteLine("echo {0} > {1}", readData.strId, readData.strMsg);

                        //// 패킷 에코 응답
                        //_JXPTC_SC_ANS_CHAT_MSG writeData = new _JXPTC_SC_ANS_CHAT_MSG();
                        //writeData.strId = readData.strId;
                        //writeData.strMsg = readData.strMsg;
                        //COutPacket.WRITE_PACKET((ushort)EJxSessionProtocol.JXPTC_SC_ANS_CHAT_MSG, writeData, this.GetWriteQueue());
                        //this.SendPacket();
                    }
                    break;

                case EJxSessionProtocol.JXPTC_SC_NFY_CHAT_MSG:
                    {
                        // 패킷을 파싱한다.
                        _JXPTC_SC_ANS_CHAT_MSG readData = CInPacket.READ_PACKET<_JXPTC_SC_ANS_CHAT_MSG>(_IncomingPacket);

                        Console.WriteLine("broadcast {0} > {1}", readData.strId, readData.strMsg);
                    }
                    break;

                default:
                    Console.WriteLine("Invalid Packet : {0}", eProtocol);
                    break;
            }

            return true;
        }

        // 세션이 끊겼을 때를 처리한다.
        public override void DisconnectSession()
        {
            Console.WriteLine("DisconnectSession : {0}", this.GetHandlerStatus());     // m_eHandlerStatus로 세션의 종료 상태를 알 수 있다.
        }
    }

    class CJxConnector : CNetConnector<CJxSession>
    {
        // 세션의 생성 전략을 정의한다.
        public override CJxSession MakeHandler()
        {
            return new CJxSession(JxConfig.JXSESSION_BUFFER_SIZE);
        }

        // 세션의 연결이 완료되었을 때 처리를 정의한다.
        public override void MakeHandlerComplete(CJxSession _jxSession)
        {
            Console.WriteLine("서버에 접속되었습니다.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            CReactor.Instance.Initialize();     // Reactor 초기화

            CJxSession jxSession = null;
            CJxConnector jxConnector = new CJxConnector();
            if(NxGlobal._SUCCESS == jxConnector.BeginConnect("127.0.0.1", 8080))
            {
                jxSession = jxConnector.GetConnectedSession();

                CConsoleCommand consoleCommand = new CConsoleCommand();

                while (true)
                {
                    string strHit = String.Empty;

                    if (consoleCommand.GetKeboardHit(out strHit))
                    {
                        if (strHit == "exit")
                            break;

                        jxSession.SendReqChatMsg("MyId3", strHit);
                    }
                    else
                    {
                        jxSession.Process();
                    }
                }
            }
            else
            {
                Console.WriteLine("서버에 접속할 수 없습니다.");
            }

            CReactor.Instance.Cancel();
        }
    }
}
