using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string layoutPath = @"c:\AntiGravity Proyectos\GestionQ\src\GestionQ.Web\Views\Shared\_Layout.cshtml";
        string layoutContent = File.ReadAllText(layoutPath, Encoding.UTF8);
        string newMenuItem = @"<a asp-controller=""Purchases"" asp-action=""Index"" style=""color: #60a5fa;"">
                            <i class=""fa-solid fa-truck-ramp-box""></i> Compras (Ingreso)
                        </a>
                    </li>
                    <li>
                        <a asp-controller=""Suppliers"" asp-action=""Index"" style=""color: #f59e0b;"">
                            <i class=""fa-solid fa-truck-field""></i> Proveedores (Cta Cte)
                        </a>";
        layoutContent = layoutContent.Replace(@"<a asp-controller=""Purchases"" asp-action=""Index"" style=""color: #60a5fa;"">
                            <i class=""fa-solid fa-truck-ramp-box""></i> Compras (Ingreso)
                        </a>", newMenuItem);
        File.WriteAllText(layoutPath, layoutContent, new UTF8Encoding(false));

        string indexPath = @"c:\AntiGravity Proyectos\GestionQ\src\GestionQ.Web\Views\Suppliers\Index.cshtml";
        string indexContent = File.ReadAllText(indexPath, Encoding.UTF8);
        indexContent = indexContent.Replace(@"<a asp-action=""Edit""", @"<a asp-action=""Details"" asp-route-id=""@item.Id"" class=""btn-primary"" style=""padding: 0.4rem 0.75rem; background: var(--accent); border: none;"">Ver Cuenta</a> <a asp-action=""Edit""");
        File.WriteAllText(indexPath, indexContent, new UTF8Encoding(false));
    }
}
