using SkiaSharp.Views.Desktop;
using SkiaUiLibrary.Widgets;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

 class Transaction
{
    public string TransactionId { get; set; }
    public string Client { get; set; }
    public string Status { get; set; }
    public string Amount { get; set; }
}

const string json = """
    [
      { "transaction_id": "#TRX-9821", "client": "Nebula Corp", "status": "Completed",  "amount": "$12,450.00" },
      { "transaction_id": "#TRX-9822", "client": "Stark Ind",   "status": "Processing", "amount": "$4,200.50"  },
      { "transaction_id": "#TRX-9823", "client": "Wayne Ent",   "status": "Failed",     "amount": "$850.00"    },
      { "transaction_id": "#TRX-9824", "client": "Cyberdyne",   "status": "Completed",  "amount": "$22,100.00" }
    ]
    """;

var table = new Table("Recent Transactions", json);

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
