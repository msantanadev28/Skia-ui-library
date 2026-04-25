using SkiaSharp.Views.Desktop;
using SkiaUiLibrary.Widgets;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var transactions = new List<Transaction>
{
    new("#TRX-9821", "Nebula Corp", "Completed",  "$12,450.00"),
    new("#TRX-9822", "Stark Ind",   "Processing", "$4,200.50"),
    new("#TRX-9823", "Wayne Ent",   "Failed",      "$850.00"),
    new("#TRX-9824", "Cyberdyne",   "Completed",   "$22,100.00"),
};

//record Client (string Name, string Email, string Phone);
var clients = new List<Client>
{
    new("Nebula Corp", "nebula@example.com", "555-1234"),
    new("Stark Ind",   "stark@example.com",   "555-5678"),
    new("Wayne Ent",   "wayne@example.com",   "555-9012"),
    new("Cyberdyne",   "cyber@example.com",  "555-3456"),
};



var table = Table.From("Customers", clients);

var form = new Form
{
    Text = "Recent Transactions",
    ClientSize = new System.Drawing.Size(1100, table.PreferredHeight),
    FormBorderStyle = FormBorderStyle.FixedSingle,
    MaximizeBox = false,
    StartPosition = FormStartPosition.CenterScreen
};

var skControl = new SKControl { Dock = DockStyle.Fill };
skControl.PaintSurface += (_, e) => table.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);

form.Controls.Add(skControl);
Application.Run(form);

record Transaction(string TransactionId, string Client, string Status, string Amount);
record Client (string Name, string Email, string Phone);
