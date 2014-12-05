#define BROADCAST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using net_x;
using net_x.proactor;

namespace BasicServer
{
    // 구성 클래스
    public class JxConfig
    {
        public static readonly int JXSESSION_BUFFER_SIZE = 512;                // 세션 버퍼 사이즈
        public static readonly int JXSESSION_CONNECTED_COUNT = 1000;           // 최대 동시 접속자 수
    }

    // 프로토콜 
    public enum EJxSessionProtocol : ushort
    {
        JXPTC_CS_REQ_CHAT_MSG = 1,      // 채팅 요청
        JXPTC_SC_ANS_CHAT_MSG = 2,      // 채팅 응답
        JXPTC_SC_NFY_CHAT_MSG = 3,      // 채팅 통지
    }

    // 프로토콜 정의 클래스
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

    // 연결을 상징하는 세션 클래스
    public class CJxSession : CNetSession
    {
        // 버퍼 사이즈와 버퍼 카운트를 설정해서 베이스 클래스에 넘긴다.
        public CJxSession(int _iBufferSize, int _iBufferCount = 2)
            : base(_iBufferSize, _iBufferCount)
        {
        }

        // 메시지 전송
        private void SendAnsChatMsg(string _strId, string _strMsg)
        {
            _JXPTC_SC_ANS_CHAT_MSG writeData = new _JXPTC_SC_ANS_CHAT_MSG();
            writeData.strId = _strId;
            writeData.strMsg = _strMsg;
            COutPacket.WRITE_PACKET((ushort)EJxSessionProtocol.JXPTC_SC_ANS_CHAT_MSG, writeData, this.GetWriteQueue());
            this.SendPacket();
        }

        // 들어온 패킷을 처리한다.
        public override bool IncomingPacketProcess(CInPacket _IncomingPacket)
        {
            // 처리할 프로토콜을 알아낸다.
            EJxSessionProtocol eProtocol = (EJxSessionProtocol)_IncomingPacket.GetProtocol();

            switch (eProtocol)
            {
                case EJxSessionProtocol.JXPTC_CS_REQ_CHAT_MSG:      // 채팅 요청 패킷이라면
                    {
                        // 처리할 해당 클래스로 파싱한다.
                        _JXPTC_CS_REQ_CHAT_MSG readData = CInPacket.READ_PACKET<_JXPTC_CS_REQ_CHAT_MSG>(_IncomingPacket);

#if (BROADCAST)
                        // 이서버는 에코 서버이기 때문에 다시 응답 패킷을 보낸다.
                        CJxConnectedManager.Instance.SendNfyChatMsg(readData.strId, readData.strMsg);
#else
                        this.SendAnsChatMsg(readData.strId, readData.strMsg);
#endif

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

            // 연결 매니저에서 제거한다.
            CJxConnectedManager.Instance.DelSession(this);
        }

  //      public void SetConnectedSlotIndex(int _iConnectedSlotIndex) { m_iConnectedSlotIndex = _iConnectedSlotIndex; }

 //       public int GetConnectedSlotIndex() { return m_iConnectedSlotIndex; }

        private int m_iConnectedSlotIndex;      //  관리되는 세션 인덱스
    }

    // 세션 연결을 기다리는 accept 클래스
    public class CJxAcceptor : CNetAcceptor<CJxSession>
    {
        // 세션의 생성 전략을 정의한다.
        public override CJxSession MakeHandler()
        {
            return new CJxSession(JxConfig.JXSESSION_BUFFER_SIZE);
        }

        // 세션의 연결이 완료되었을 때 처리를 정의한다.
        public override void MakeHandlerComplete(CJxSession _jxSession)
        {
            CJxConnectedManager.Instance.AddSession(_jxSession);
        }        
    }

    // 세션을 관리하는 클래스로서 전역적으로 관리하기 위해서 싱글톤을 적용하였다.
    // 서버는 동시에 여러 세션을 처리하게 된다. 그러므로 세션이 연결되면 이것을 관리할 클래스가 필요하다.
    public class CJxConnectedManager
    {
        static readonly CJxConnectedManager m_singleInstance = new CJxConnectedManager();
        static CJxConnectedManager() {}
        public static CJxConnectedManager Instance
        {
            get
            {
                return m_singleInstance;
            }
        }

        public void Initialize()
        {
            for (int iIdx = 0; iIdx < JxConfig.JXSESSION_CONNECTED_COUNT; ++iIdx)
            {
                m_aJxSession[iIdx] = null;
            }
        }

        // 서버는 여러 세션을 처리한다. 다음과 같이 순회를 돌면서 세션의 패킷을 처리해 준다.
        // 종료할 때까지 처리할 패킷이 있는지 세션의 Process()함수를 들여다보고 처리한다.
        // 내부적으로 Process()함수에서 처리할 패킷이 있다면 세션의 IncomingPacketProcess()가 
        // 호출되게 된다.
        public void PacketProcess()
        {
            lock (m_guardLock)
            {
                for (int iIdx = 0; iIdx < JxConfig.JXSESSION_CONNECTED_COUNT; ++iIdx)
                {
                    CJxSession jxSessioin = m_aJxSession[iIdx];
                    if (jxSessioin != null)
                        jxSessioin.Process();
                }
            }
        }

        // 세션 추가
        public void AddSession(CJxSession _jxSession)
        {
            lock (m_guardLock)
            {
                if (m_aJxSession[iIndex] == null)
                {
                    m_aJxSession[iIndex++] = _jxSession;

                    iIndex = iIndex % JxConfig.JXSESSION_CONNECTED_COUNT;
                }
            }
        }

        // 세션 삭제
        public void DelSession(CJxSession _jxSession)
        {
            lock (m_guardLock)
            {
                for (int iIdx = 0; iIdx < JxConfig.JXSESSION_CONNECTED_COUNT; ++iIdx)
                {
                    if (_jxSession == m_aJxSession[iIdx])
                    {
                        m_aJxSession[iIdx] = null;
                        break;
                    }
                }
            }
        }

        // Send Method

        // SessionBroadCast
        public void SendNfyChatMsg(string _strId, string _strMsg)
        {
            _JXPTC_SC_NFY_CHAT_MSG writeData = new _JXPTC_SC_NFY_CHAT_MSG();
            writeData.strId = _strId;
            writeData.strMsg = _strMsg;
            COutPacket outPacket = COutPacket.WRITE_PACKET((ushort)EJxSessionProtocol.JXPTC_SC_NFY_CHAT_MSG, writeData, m_shareQueue);

            lock (m_guardLock)
            {
                for (int iIdx = 0; iIdx < JxConfig.JXSESSION_CONNECTED_COUNT; ++iIdx)
                {
                    CJxSession jxSession = m_aJxSession[iIdx];
                    if (null != jxSession)
                    {
                        jxSession.SendPacket(outPacket);
                    }
                }
            }
        }

        private object m_guardLock = new object();      // 동기화 오브젝트
        private CJxSession[] m_aJxSession = new CJxSession[JxConfig.JXSESSION_CONNECTED_COUNT];     // 연결된 세션의 관리
        private int iIndex = 0;
        private CShareQueue m_shareQueue = new CShareQueue(JxConfig.JXSESSION_BUFFER_SIZE);
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server Start...");

            CProactor.Instance.Initialize();        // Proactor 초기화

            CJxAcceptor jxAcceptor = new CJxAcceptor();
            if (NxGlobal._FAILED == jxAcceptor.BeginAccept(8080))
            {
                Console.WriteLine("Bind Error");
            }
            else
            {
                Console.WriteLine("...... [OK]");
            }

            CConsoleCommand consoleCommand = new CConsoleCommand();

            while (true)
            {
                string strHit = String.Empty;

                if (consoleCommand.GetKeboardHit(out strHit))
                {
                    if (strHit == "exit")
                        break;
                }
                else
                {
                    CJxConnectedManager.Instance.PacketProcess();       // 세션의 패킷을 처리한다.
                }
            }

            Console.WriteLine("Shutdown");

            CProactor.Instance.Cancel();
            
        }
    }
}
