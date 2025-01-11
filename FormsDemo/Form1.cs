using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FormsDemo
{
    public partial class Form1 : Form
    {
        private readonly IDistributedCache _cache;
        public Form1(
            ILogger<Form1> logger,
            IDistributedCache cache)
        {
            _cache = cache;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _cache.SetString("1", "v1");
        }
    }
}
