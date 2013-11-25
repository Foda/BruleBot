using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BruleBot
{
    public struct IRCConfig
    {
        public string Server;
        public int Port;

        public string Nick;
        public string Name;
        public string Chan;
        public string Password;
    }

    public class IRCBot
    {
        TcpClient IRCConnection = null;
        IRCConfig Config;
        Stream NetStream = null;
        StreamReader StreamRead = null;
        StreamWriter StreamWrite = null;

        Timer responseTimer;
        Timer delayTalkTimer; //To prevent spamming
        Timer randomQuestionTimer;
        Timer resetMemoryTimer;
        Random rand;

        Dictionary<string, bool> responseMemory = new Dictionary<string, bool>();
        Dictionary<string, string> responseDictionary = new Dictionary<string, string>();
        List<string> randomQuestions = new List<string>();

        private bool _canRespond = false;
        private bool _hasJoined = false;

        public IRCBot(IRCConfig config)
        {
            Config = config;

            rand = new Random();

            responseTimer = new Timer(33);
            responseTimer.AutoReset = true;
            responseTimer.Elapsed += responseTimer_Elapsed;

            delayTalkTimer = new Timer(2000);
            delayTalkTimer.AutoReset = true;
            delayTalkTimer.Elapsed += delayTalkTimer_Elapsed;

            randomQuestionTimer = new Timer(rand.Next(10000, 30000));
            randomQuestionTimer.AutoReset = true;
            randomQuestionTimer.Elapsed += randomQuestionTimer_Elapsed;

            resetMemoryTimer = new Timer(6000);
            resetMemoryTimer.AutoReset = true;
            resetMemoryTimer.Elapsed += resetMemoryTimer_Elapsed;

            InitResponses();

            try
            {
                IRCConnection = new TcpClient(Config.Server, Config.Port);
            }
            catch
            {
                Console.WriteLine("Connection Error");
            }

            try
            {
                NetStream = IRCConnection.GetStream();

                if (true)
                {
                    SslStream sslStream = new SslStream(NetStream, false, delegate
                    {
                        return true;
                    });
                    sslStream.AuthenticateAsClient(Config.Server);
                    NetStream = sslStream;
                }

                StreamRead = new StreamReader(NetStream);
                StreamWrite = new StreamWriter(NetStream);

                SendData("PASS", Config.Password);
                SendData("USER", Config.Nick + " yablew.it " + " yablew.it" + " :" + Config.Name);
                SendData("NICK", Config.Nick);

                //string cmd = Console.ReadLine();
                

                responseTimer.Start();
                randomQuestionTimer.Start();
                resetMemoryTimer.Start();
            }
            catch
            {
                Console.WriteLine("Communication error");
                responseTimer.Stop();
            }
        }

        //Randomly reset one of our responses
        void resetMemoryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_hasJoined)
            {
                SendData("JOIN", Config.Chan);
                _hasJoined = true;

                delayTalkTimer.Start();
            }

            List<string> keys = new List<string>(responseMemory.Keys);

            if (keys.Count > 0)
            {
                string randomKey = keys[rand.Next(0, keys.Count)];
                responseMemory[randomKey] = false;
            }
        }

        //Randomly ask a question
        void randomQuestionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            randomQuestionTimer.Interval = rand.Next(30000, 60000);
            //SendData("PRIVMSG " + Config.Chan, randomQuestions[rand.Next(0, randomQuestions.Count() - 1)]);
        }

        private void InitResponses()
        {
            AddResponse("Microsoft", "did you know Bill Grates invented Michaelsoft? wouldn't it be cool if he could remember my dingus password for my email?");
            AddResponse("mistake", "why would you do that yah dingus?");
            AddResponse("accident", "i wish i was in a lover's trance...");
            AddResponse("difficult", "sounds as easy as eating a peanutbutter sandwitch!");
            AddResponse("xmas", "my secret recipe for eggnog: take 2 eggs and crack them on your noggin. eggnog.");
            AddResponse("bees", "why not honey bees?");
            AddResponse("shit", "My fiber intake keeps the toilet paper companies in business.");
            AddResponse("duck", "Duckbutter?");
            AddResponse("health", "for your health!");
            AddResponse("what", "...you know what I meant you stupid turds.");
            AddResponse("heat", "Beat the heat with frozen meat treats!");
            AddResponse("cookie", "I just realized that I dont have any cookies! I guess the Santa man will fly by my fire pits. Maybe I'll just take my neighbors cookies.");
            AddResponse("weather", "It's raining flubbers!");
            AddResponse("bacon", "I have bacon, yes I do.");
            AddResponse("dingus", "here's a doohickey—and there's the dingus");
            AddResponse(".NET", "why use .net when you can buy a .com ya dingus?");
            AddResponse("python", "my mother wrote snakes once, who cares?");
            AddResponse("java", "you cant program with coffee ya dingus");

            randomQuestions.Add("Who's your fravorite basketball player?");
            randomQuestions.Add("Where is your favorite place to eat a corndog?");
            randomQuestions.Add("Why is the domestic product so gross?");
            randomQuestions.Add("Ever put your sock on upside down?");
            randomQuestions.Add("If he has ice water in his veins, does that mean he never gets thirsty?");
        }

        public void SendData(string cmd, string param)
        {
            if (param == null)
            {
                StreamWrite.WriteLine(cmd);
                StreamWrite.Flush();
                Console.WriteLine(cmd);
            }
            else
            {
                StreamWrite.WriteLine(cmd + " " + param);
                StreamWrite.Flush();
                Console.WriteLine(cmd + " " + param);
            }
        }

        private bool IsKeywordInMemory(string keyword)
        {
            if (!responseMemory.ContainsKey(keyword))
            {
                //Not in memory, so add it
                responseMemory.Add(keyword, false);
                return false;
            }
            else if (responseMemory.ContainsKey(keyword))
            {
                return responseMemory[keyword];
            }

            return false;
        }

        public void AddResponse(string keyword, string response)
        {
            responseDictionary.Add(keyword, response);
        }

        public void FireResponse(string keyword)
        {
            if (!IsKeywordInMemory(keyword))
            {
                if (responseDictionary.ContainsKey(keyword))
                {
                    responseMemory[keyword] = true;
                    SendData("PRIVMSG " + Config.Chan, responseDictionary[keyword]);
                    _canRespond = false;
                }
            }
        }

        void responseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Ghetto thread locking
            responseTimer.Stop();

            string[] ex;
            string data;

            data = StreamRead.ReadLine();
            Console.WriteLine(data);

            char[] charSeparator = new char[] { ' ' };
            ex = data.Split(charSeparator, 5);

            if (ex[0] == "PING")
            {
                SendData("PONG", ex[1]);
            }

            if (_canRespond)
            {
                //Check it out!
                if (data.Contains("microsoft") || data.Contains("ms") || data.Contains("M$") || data.Contains("Microsoft"))
                {
                    FireResponse("Microsoft");
                }
                else if (data.Contains("mistake"))
                {
                    FireResponse("mistake");
                }
                else if (data.Contains("accident"))
                {
                    FireResponse("accident");
                }
                else if (data.Contains("difficult") || data.Contains("difficulty"))
                {
                    FireResponse("difficult");
                }
                else if (data.Contains("xmas") || data.Contains("christmas") || data.Contains("holiday") || data.Contains("eggnog"))
                {
                    FireResponse("xmas");
                }
                else if (data.Contains("shit"))
                {
                    FireResponse("shit");
                }
                else if (data.Contains("bees"))
                {
                    FireResponse("bees");
                }
                else if (data.Contains("duck") || data.Contains("duckduckgo") || data.Contains("duckduck") || data.Contains("dick"))
                {
                    FireResponse("duck");
                }
                else if (data.Contains("health"))
                {
                    FireResponse("health");
                }
                else if (data.Contains("what does") || data.Contains("don't understand"))
                {
                    FireResponse("what");
                }
                else if (data.Contains("too hot") || data.Contains("heat"))
                {
                    FireResponse("heat");
                }
                else if (data.Contains("cookie") || data.Contains("cookies") || data.Contains("oreo") || data.Contains("candy") || data.Contains("santa"))
                {
                    FireResponse("cookie");
                }
                else if (data.Contains("weather") || data.Contains("weather?"))
                {
                    FireResponse("weather");
                }
                else if (data.Contains(".NET") || data.Contains(".net"))
                {
                    FireResponse(".NET");
                }
                else if (data.Contains("java") || data.Contains("Java"))
                {
                    FireResponse("java");
                }
                else if (data.Contains("python") || data.Contains("Python"))
                {
                    FireResponse("python");
                }
                else if (data.Contains("bacon"))
                {
                    FireResponse("bacon");
                }
                else if (data.Contains("dingus"))
                {
                    FireResponse("dingus");
                }
            }

            responseTimer.Start();
        }

        void delayTalkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _canRespond = true;
        }
    }
}
