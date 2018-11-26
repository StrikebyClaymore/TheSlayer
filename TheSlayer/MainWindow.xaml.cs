using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Timers;

namespace TheSlayer {

    class MyKeyPress
    {
        public bool Space = false;
    }

    public class Mobs
    {
        public Position position = new Position();
        public Image image = new Image();
        public Image attackImage = new Image();
        public Image blockImage = new Image();
        public Image corpse = new Image();
        public Image targetBossImage = new Image();
        public Image bigAttackImage = new Image();
        public string Type;
        public bool Life = true;
        public int Speed;
        public int Hp;
        public int Damage;
        public int AttackSpeed;
        public int AttackColdown;
        public int AttackDistance;
        public int SuperAttackColdown;
        public int SuperAttackSpeed;
        public int SuperAttackDistance;
        public bool block = false;
        public int blockColdown;
        public int blockSpeed;
        public int dir;
        public List<Mobs> targetList = new List<Mobs>{};

        public Mobs(string str, int x, int y)
        {
            if (str == "Monster")
            {
                Type = str;
                image.Source = new BitmapImage(new Uri("imgs/mob1.png", UriKind.RelativeOrAbsolute));
                attackImage.Source = new BitmapImage(new Uri("imgs/monsterAttack.png", UriKind.RelativeOrAbsolute));
                corpse.Source = new BitmapImage(new Uri("imgs/mob1Corpse.png", UriKind.RelativeOrAbsolute));
                Hp = 100;
                Speed = 1;
                Damage = 1;
                AttackSpeed = 100;
                AttackColdown = 0;
                dir = 1;
            }
            if (str == "Boss")
            {
                Type = str;
                image.Source = new BitmapImage(new Uri("imgs/boss1.png", UriKind.RelativeOrAbsolute));
                attackImage.Source = new BitmapImage(new Uri("imgs/bossAttack.png", UriKind.RelativeOrAbsolute));
                targetBossImage.Source = new BitmapImage(new Uri("imgs/targetBoss1.png", UriKind.RelativeOrAbsolute));
                bigAttackImage.Source = new BitmapImage(new Uri("imgs/bossBigAttack.png", UriKind.RelativeOrAbsolute));
                Hp = 450;
                Speed = 1;
                Damage = 1;
                AttackSpeed = 100;
                AttackColdown = 0;
                SuperAttackColdown = 0;
                SuperAttackSpeed = 300;
                SuperAttackDistance = 64;
                dir = 1;
            }
            if (str == "Player")
            {
                Type = str;
                image.Source = new BitmapImage(new Uri("imgs/warman.png", UriKind.RelativeOrAbsolute));
                attackImage.Source = new BitmapImage(new Uri("imgs/swordAttack.png", UriKind.RelativeOrAbsolute));
                blockImage.Source = new BitmapImage(new Uri("imgs/block.png", UriKind.RelativeOrAbsolute));
                Hp = 3;
                Speed = 4;
                Damage = 50;
                AttackSpeed = 50;
                AttackColdown = 0;
                AttackDistance = 24;
                blockColdown = 0;
                blockSpeed = 50;
                dir = 0;
            }
            position.x = x;
            position.y = y;
        }
    }

    public class StaticObject {
        public Position position = new Position();
        public Image image = new Image();

        public StaticObject(string str, int x, int y)
        {
            if (str == "Heart")
            {
                image.Source = new BitmapImage(new Uri("imgs/heart.png", UriKind.RelativeOrAbsolute));
            }
            position.x = x;
            position.y = y;
        }
    }

    public class Position
    {
        public double x;
        public double y;
            
        public void SetPosition(double _x, double _y)
        {
            x = _x;
            y = _y;
        }
    }
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        MyKeyPress KeyPress = new MyKeyPress();

        public bool GameStatus = false;

        public List<Mobs> Moblist = new List<Mobs> { };
        public List<StaticObject> ObjList = new List<StaticObject> { };

        Mobs WarMan = new Mobs("Player", 100, 240);

        public int glory = 0;

        public MainWindow()
        {
            InitializeComponent();
            MessageBox.Show("Перед началом игры ответь на вопрос: Ты пидор?", "Важный вопрос", MessageBoxButton.YesNo);
        }

        async Task Invalidate()
        {

            foreach (Mobs mob in Moblist)
            {
                Canvas.SetLeft(mob.image, mob.position.x);
            }
            await Task.Delay(1);
        }

        public void InitGame()
        {
            GameStatus = true;

            MakeGUI();
            SpawnEnemies(0);
            InitCanvas(WarMan);
            Moblist.Add(WarMan);

            //MyFunc();
            UpdateCycle();
        }

        public async void UpdateCycle()
        {
            while (GameStatus)
            {
                await Task.Delay(1);
                AI();
                UpdateCanvas();
            }
        }

        public void UpdateStats()
        {
            Score.Text = "Glory: " + Convert.ToString(glory);
        }

        public async Task Attack(Mobs mob = null, int? type = null)
        {
            if (mob != null)
            {
                if (type == 0 && mob.AttackColdown <= 0)
                {
                    if (WarMan.block)
                    {
                        AttackAnim(mob);
                        BlockDown();
                        mob.AttackColdown = mob.AttackSpeed;
                    }
                    else
                    {
                        AttackAnim(mob);
                        GetDamage(mob, 0);
                        mob.AttackColdown = mob.AttackSpeed;
                    }
                }
            }
            else
            {
                if (WarMan.AttackColdown <= 0)
                {
                    BlockDown();
                    GetTarget();
                    if (WarMan.targetList.Count > 0)
                    {
                        AttackAnim(WarMan);
                        foreach (Mobs target in WarMan.targetList)
                        {
                            GetDamage(target, 1);
                            WarMan.AttackColdown = WarMan.AttackSpeed;
                        }
                    }
                    else
                    {
                        AttackAnim(WarMan);
                        WarMan.AttackColdown = WarMan.AttackSpeed;
                    }
                }
            }
        }

        public async Task BossAttack(Mobs mob)
        {
            if (mob.SuperAttackColdown <= 0 && DistTo(mob, WarMan, 0).x <= mob.SuperAttackDistance+32)
            {
                mob.SuperAttackColdown = mob.SuperAttackSpeed;
                await AttackAnim(mob, mob.targetBossImage, 1000);
                await Task.Delay(300);
                AttackAnim(mob, mob.bigAttackImage, 900);
                if (DistTo(mob, WarMan, 0).x <= mob.SuperAttackDistance)
                {
                    GetDamage(mob, 0);
                } 
            }
            else if (mob.SuperAttackColdown >= mob.SuperAttackSpeed/4 && mob.SuperAttackColdown <= mob.SuperAttackSpeed/2 && DistTo(mob, WarMan, 0).x <= mob.AttackDistance + 32 && mob.AttackColdown <= 0)
            {
                Attack(mob, 0);
            }
        }

        public void GetTarget()
        {
            WarMan.targetList.Clear();
            foreach (Mobs mob in Moblist)
            {
                if (mob == WarMan)
                {
                    continue;
                }
                if (DistTo(mob, WarMan, 0).x <= WarMan.AttackDistance)//&& DistTo(mob, WarMan, 0).y <= WarMan.AttackDistance
                {
                    WarMan.targetList.Add(mob);
                }
                else
                {
                    WarMan.targetList.Remove(mob);
                }
            }
        }

        public async Task AttackAnim(Mobs mob = null, Image img = null, int ?time = null)
        {
            if (mob != null && img == null)
            {
                MyCanvas.Children.Add(mob.attackImage);
                mob.attackImage.Stretch = Stretch.None;
                if (mob.dir == 0)
                {
                    mob.attackImage.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                    Canvas.SetLeft(mob.attackImage, mob.position.x + 8);
                }
                else
                {
                    mob.attackImage.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                    Canvas.SetLeft(mob.attackImage, mob.position.x - 8);
                }
                if (mob.Type == "Boss")
                {
                    Canvas.SetTop(mob.attackImage, mob.position.y + 32);
                }
                else
                {
                    Canvas.SetTop(mob.attackImage, mob.position.y);
                }
                await Task.Delay(200);
                MyCanvas.Children.Remove(mob.attackImage);
            }
            else
            {
                MyCanvas.Children.Add(img);
                img.Stretch = Stretch.None;
                if (mob.dir == 0)
                {
                    img.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                    Canvas.SetLeft(img, mob.position.x + 32);
                }
                else
                {
                    img.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                    Canvas.SetLeft(img, mob.position.x - 32);
                }
                Canvas.SetTop(img, mob.position.y + 48);
                await Task.Delay(Convert.ToInt32(time));
                MyCanvas.Children.Remove(img);
            }
        }

        public void BlockUp()
        {
            if (WarMan.blockColdown <= 0)
            {
                WarMan.block = true;
                InitCanvas(null, null, WarMan.blockImage);
                WarMan.blockColdown = WarMan.blockSpeed;
            }
        }
        public void BlockDown()
        {
            WarMan.block = false;
            DeleteCanvasItem(null, null, WarMan.blockImage);
        }

        public async Task GetDamage(Mobs mob, int type)
        {
            if (type == 0)
            {
                WarMan.Hp -= mob.Damage;
                DeleteCanvasItem(null, ObjList[WarMan.Hp]);
                if (WarMan.Hp <= 0)
                {
                    Die();
                }
            }
            else
            {
                mob.Hp -= WarMan.Damage;
                if (mob.Hp <= 0)
                {
                    if (mob.Type == "Monster")
                    {
                        glory += 1;
                        UpdateStats();
                        InitCanvas(mob, null, mob.corpse);
                        DeleteCanvasItem(mob);
                        WarMan.targetList.Remove(mob);
                    }
                    else if (mob.Type == "Boss")
                    {
                        Win();
                    }
                }

            }
        }

        public async Task Win()
        {
            glory += 10;
            UpdateStats();

            GameStatus = false;

            LabelText.Visibility = Visibility.Visible;
            LabelText.Foreground = System.Windows.Media.Brushes.Blue;
            LabelText.FontSize = 16;
            LabelText.Content = "Вы выиграли! Поздравляю!!";

            await Task.Delay(10000);

            StartMenu.Visibility = Visibility.Visible;
            LabelText.Visibility = Visibility.Hidden;

            Moblist.Clear();
            ObjList.Clear();
            MyCanvas.Children.Clear();
        }

        public async Task Die()
        {
            //string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //System.Diagnostics.Process.Start(path);
            //Process.GetCurrentProcess().Kill();

            GameStatus = false;

            LabelText.Foreground = System.Windows.Media.Brushes.Red;
            LabelText.FontSize = 16;
            LabelText.Visibility = Visibility.Visible;
            LabelText.Content = "Вы проиграли! Попробуйте ещё раз ;)";

            await Task.Delay(2000);

            StartMenu.Visibility = Visibility.Visible;

            LabelText.Visibility = Visibility.Hidden;
            Moblist.Clear();
            ObjList.Clear();
            MyCanvas.Children.Clear();
        }

        public void Reset()
        {
            WarMan.Hp = 3;
            WarMan.position.x = 100;
            WarMan.position.y = 240;
        }

        public async Task AI()
        {
            if (ObjList.Count < 1)
            {
                return;
            }
            foreach (Mobs mob in Moblist)
            {
                if (mob.AttackColdown >= 0)
                {
                    mob.AttackColdown -= 1;
                }
                if (mob == WarMan)
                {
                    if (!WarMan.block && WarMan.blockColdown >= 0)
                    {
                        WarMan.blockColdown -= 1;
                    }
                    GetTarget();
                    continue;
                }
                else if (mob.Type == "Boss")
                {
                    if (mob.SuperAttackColdown >= 0)
                    {
                        mob.SuperAttackColdown -= 1;
                    }
                    if (DistTo(mob, WarMan, 0).x > mob.AttackDistance+32)// || DistTo(mob, WarMan, 0).y > 32
                    {
                        await Task.Delay(300);
                        MoveTo(mob);
                    }
                    else
                    {
                        if (mob.Hp < 250)
                        {
                            BossAttack(mob);
                        }
                        else
                        {
                            Attack(mob, 0);
                        }
                    }
                }
                else if (DistTo(mob, WarMan, 0).x > 32)// || DistTo(mob, WarMan, 0).y > 32
                {
                    await Task.Delay(300);
                    MoveTo(mob);
                }
                else
                {
                    await Task.Delay(500);
                    Attack(mob, 0);
                }
            }
        }

        public async Task MoveTo(Mobs mob)
        {
            if (DistTo(mob, WarMan, 1).x > 16)
            {
                mob.position.x -= mob.Speed;
                mob.dir = 1;
            }
            else if (DistTo(mob, WarMan, 1).x < -16)
            {
                mob.position.x += mob.Speed;
                mob.dir = 0;
            }
           /* if (DistTo(mob, WarMan, 1).y > 16)
            {
                mob.position.y -= mob.Speed;
            }
            else if (DistTo(mob, WarMan, 1).y < -16)
            {
                mob.position.y += mob.Speed;
            }*/
        }


        public void MakeGUI()
        {
            Score.Text = "Glory: " + Convert.ToString(glory);

            for (int i = 0; i < 3; i++)
            {
                StaticObject h = new StaticObject("Heart", 8 + 20 * (i), 8);
                InitCanvas(null, h);
                ObjList.Add(h);
            }
        }

        public void SpawnEnemies(int type)
        {
            if (type == 0)
            {
                Mobs enemie1 = new Mobs("Monster", 300, 240);
                Moblist.Add(enemie1);
                InitCanvas(enemie1);
            }
            else
            {
                Mobs enemie1 = new Mobs("Boss", 350, 208);
                Moblist.Add(enemie1);
                InitCanvas(enemie1);
            }
        }

        public Position DistTo(Mobs self, Mobs target, int type)
        {
            Position dist = new Position();

            if (type == 0)
            {
                dist.x = Math.Abs(self.position.x - target.position.x);
                dist.y = Math.Abs(self.position.y - target.position.y);
            }
            else
            {
                dist.x = self.position.x - target.position.x;
                dist.y = self.position.y - target.position.y;
            }
            return dist;
        }

        public void InitCanvas(Mobs mob = null, StaticObject obj = null, Image img = null)
        {
            if (mob != null && img == null)
            {
                MyCanvas.Children.Add(mob.image);
                mob.image.Stretch = Stretch.None;
                if (mob.dir == 0)
                {
                    mob.image.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                }
                else { mob.image.FlowDirection = System.Windows.FlowDirection.RightToLeft; }
                Canvas.SetLeft(mob.image, mob.position.x);
                Canvas.SetTop(mob.image, mob.position.y);
            }
            else if (obj != null)
            {
                MyCanvas.Children.Add(obj.image);
                obj.image.Stretch = Stretch.None;
                Canvas.SetLeft(obj.image, obj.position.x);
                Canvas.SetTop(obj.image, obj.position.y);
            }
            else if (img != null && mob == null)
            {
                MyCanvas.Children.Add(img);
                img.Stretch = Stretch.None;
                if (WarMan.dir == 0)
                {
                    img.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                }
                else { img.FlowDirection = System.Windows.FlowDirection.RightToLeft; }
                Canvas.SetLeft(img, WarMan.position.x);
                Canvas.SetTop(img, WarMan.position.y);
            }
            else if (img != null && mob != null)
            {
                MyCanvas.Children.Add(img);
                img.Stretch = Stretch.None;
                if (mob.dir == 0)
                {
                    img.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                }
                else { img.FlowDirection = System.Windows.FlowDirection.RightToLeft; }
                Canvas.SetLeft(img, mob.position.x);
                Canvas.SetTop(img, mob.position.y);
            }
        }

        public void DeleteCanvasItem(Mobs mob = null, StaticObject obj = null, Image img = null)
        {
            if (mob != null)
            {
                if (mob.Type == "Monster" && glory <= 2)
                {
                    SpawnEnemies(0);
                }
                if (glory >= 3)
                {
                    SpawnEnemies(1);
                }
                MyCanvas.Children.Remove(mob.image);
                Moblist.Remove(mob);
                mob = null;
            }
            if (obj != null)
            {
                MyCanvas.Children.Remove(obj.image);
                ObjList.Remove(obj);
                obj = null;
            }
            if (img != null)
            {
                MyCanvas.Children.Remove(img);
            }
        }

        public void UpdateCanvas()
        {
            foreach (Mobs mob in Moblist)
            {
                if (mob.dir == 0)
                {
                    mob.image.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                }
                else { mob.image.FlowDirection = System.Windows.FlowDirection.RightToLeft; }
                Canvas.SetLeft(mob.image, mob.position.x);
                Canvas.SetTop(mob.image, mob.position.y);
            }
            foreach (StaticObject obj in ObjList)
            {
                Canvas.SetLeft(obj.image, obj.position.x);
                Canvas.SetTop(obj.image, obj.position.y);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (GameStatus)
            {
                //Score.Text = Convert.ToString(Canvas.GetTop(WarMan) + " " + Canvas.GetLeft(WarMan));
                base.OnKeyDown(e);
                if (e.Key == Key.W)
                {
                    //WarMan.position.y -= WarMan.Speed;
                }
                if (e.Key == Key.D)
                {
                    if (!WarMan.block)
                    {
                        WarMan.position.x += WarMan.Speed;
                        WarMan.dir = 0;
                    }
                }
                if (e.Key == Key.A)
                {
                    if (!WarMan.block)
                    {
                        WarMan.position.x -= WarMan.Speed;
                        WarMan.dir = 1;
                    }
                }
                if (e.Key == Key.S)
                {
                    //WarMan.position.y += WarMan.Speed;
                }
                if (e.Key == Key.E)
                {
                    KeyPress.Space = true;
                    BlockUp();
                }
                if (e.Key == Key.Space)
                {
                    Attack();
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.E)
            {
                BlockDown();
                KeyPress.Space = false;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            LabelText.Visibility = Visibility.Hidden;
            StartMenu.Visibility = Visibility.Hidden;
            Reset();
            InitGame();
        }
    }
}
