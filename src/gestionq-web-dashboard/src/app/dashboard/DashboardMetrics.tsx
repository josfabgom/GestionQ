"use client";
import { useState, useMemo } from "react";
import { Activity, Package, DollarSign, TrendingUp, BarChart3, ShoppingCart, Award } from "lucide-react";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";

// Función para agrupar y sumar dinamicamente a partir de rawSales
function computeDynamicStats(rawSales: any[], startDate: string, endDate: string) {
  if (!rawSales || !Array.isArray(rawSales)) return { dynamicTopProducts: null, dynamicSalesByPayment: null };

  const filteredSales = rawSales.filter((s: any) => {
    if (!s.date) return false;
    const dateStr = s.date.split('T')[0];
    if (startDate && dateStr < startDate) return false;
    if (endDate && dateStr > endDate) return false;
    return true;
  });

  // Top Products
  const productMap = new Map();
  filteredSales.forEach((s: any) => {
    if (s.items) {
      s.items.forEach((i: any) => {
        if (!productMap.has(i.productId)) {
          productMap.set(i.productId, { name: i.name, internalCode: i.internalCode, totalQuantity: 0, totalRevenue: 0 });
        }
        const p = productMap.get(i.productId);
        p.totalQuantity += i.quantity;
        p.totalRevenue += i.revenue;
      });
    }
  });
  const dynamicTopProducts = Array.from(productMap.values()).sort((a: any, b: any) => b.totalQuantity - a.totalQuantity).slice(0, 20);

  // Sales by payment (grouped by date)
  const paymentDatesMap = new Map();
  filteredSales.forEach((s: any) => {
    const dateStr = s.date.split('T')[0];
    if (!paymentDatesMap.has(dateStr)) {
      paymentDatesMap.set(dateStr, { date: dateStr });
    }
    const day = paymentDatesMap.get(dateStr);
    if (s.payments) {
      s.payments.forEach((p: any) => {
        const method = p.method || 'Efectivo';
        day[method] = (day[method] || 0) + p.amount;
      });
    }
  });
  
  // Sort by date
  const dynamicSalesByPayment = Array.from(paymentDatesMap.values()).sort((a: any, b: any) => a.date.localeCompare(b.date));

  return { dynamicTopProducts, dynamicSalesByPayment };
}

export default function DashboardMetrics({ initialData }: { initialData: any }) {
  const [startDate, setStartDate] = useState<string>(new Date().toISOString().split('T')[0]);
  const [endDate, setEndDate] = useState<string>(new Date().toISOString().split('T')[0]);

  if (!initialData) {
    return (
      <div className="flex flex-col items-center justify-center p-12 bg-white/50 backdrop-blur-sm border border-slate-200 shadow-sm rounded-2xl text-slate-500">
        <Activity className="h-10 w-10 text-slate-400 mb-4 animate-pulse" />
        <h3 className="text-lg font-medium text-slate-900">Esperando datos</h3>
        <p className="mt-1">Inicia la aplicación de escritorio para comenzar la sincronización.</p>
      </div>
    );
  }

  const { summary, posBoxes: rawPosBoxes, posHistory: rawPosHistory, topProducts: fallbackTopProducts, salesByPayment: fallbackSalesByPayment, rawSales: rawSalesData } = initialData;

  // Unwrap objects that might have been wrapped by ASP.NET Core Ok(IEnumerable) -> { value: [...], Count: X }
  const posBoxes = rawPosBoxes?.value ? rawPosBoxes.value : rawPosBoxes;
  const posHistory = rawPosHistory?.value ? rawPosHistory.value : rawPosHistory;
  const rawSales = rawSalesData?.value ? rawSalesData.value : rawSalesData;

  const globalPosStats = useMemo(() => {
    let cajasAbiertas = 0;
    let totalVentas = 0;
    let totalEfectivoEsperado = 0;
    const desglose = new Map<string, number>();

    if (Array.isArray(posBoxes)) {
      posBoxes.forEach((pos: any) => {
        if (pos.isOpen) {
          cajasAbiertas++;
          totalVentas += (pos.salesAmount || 0);
          totalEfectivoEsperado += (pos.cashBalance || 0);

          if (Array.isArray(pos.paymentBreakdown)) {
            pos.paymentBreakdown.forEach((pb: any) => {
              const method = pb.method || 'Otro';
              const current = desglose.get(method) || 0;
              desglose.set(method, current + (pb.amount || 0));
            });
          }
        }
      });
    }

    return {
      cajasAbiertas,
      totalVentas,
      totalEfectivoEsperado,
      desglose: Array.from(desglose.entries()).map(([method, amount]) => ({ method, amount })).sort((a, b) => b.amount - a.amount)
    };
  }, [posBoxes]);
  
  const { dynamicTopProducts, dynamicSalesByPayment } = useMemo(() => {
    return computeDynamicStats(rawSales, startDate, endDate);
  }, [rawSales, startDate, endDate]);

  const topProducts = (dynamicTopProducts && dynamicTopProducts.length > 0) ? dynamicTopProducts : (fallbackTopProducts?.value ? fallbackTopProducts.value : fallbackTopProducts);
  const salesByPayment = (dynamicSalesByPayment && dynamicSalesByPayment.length > 0) ? dynamicSalesByPayment : (fallbackSalesByPayment?.value ? fallbackSalesByPayment.value : fallbackSalesByPayment);

  // Formatear datos para el gráfico de barras agrupadas/apiladas
  const chartData = useMemo(() => {
    if (!salesByPayment || !Array.isArray(salesByPayment)) return [];
    
    const grouped = salesByPayment.reduce((acc: any, curr: any) => {
      const dateStr = curr.date.includes('T') ? new Date(curr.date).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' }) : new Date(curr.date + "T00:00:00").toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });
      
      if (!acc[dateStr]) {
        acc[dateStr] = { date: dateStr };
      }
      
      // Manejar la estructura vieja (method, total) y la estructura dinamica (llaves por metodo)
      if (curr.method !== undefined && curr.total !== undefined) {
         acc[dateStr][curr.method] = curr.total;
      } else {
         Object.keys(curr).forEach(k => {
            if (k !== 'date') acc[dateStr][k] = curr[k];
         });
      }
      return acc;
    }, {});
    
    return Object.values(grouped);
  }, [salesByPayment]);

  // Extraer todos los métodos de pago únicos para las barras del gráfico
  const paymentMethods = useMemo(() => {
    if (!salesByPayment || !Array.isArray(salesByPayment)) return [];
    const methods = new Set<string>();
    salesByPayment.forEach((s: any) => {
      if (s.method !== undefined) {
        methods.add(s.method);
      } else {
        Object.keys(s).forEach(k => {
           if (k !== 'date') methods.add(k);
        });
      }
    });
    return Array.from(methods);
  }, [salesByPayment]);

  // Colores para el gráfico
  const colors = ["#4f46e5", "#10b981", "#f59e0b", "#f43f5e", "#8b5cf6", "#06b6d4"];

  return (
    <div className="space-y-8">
      {/* Filtro Maestro */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 flex flex-col sm:flex-row items-center justify-between gap-4">
        <h3 className="font-bold text-slate-800 flex items-center gap-2">
          Filtro Global de Fechas
        </h3>
        <div className="flex flex-wrap items-center gap-2">
          <label className="text-sm font-medium text-slate-600">Desde:</label>
          <input 
            type="date" 
            className="border border-slate-200 rounded-lg px-2 py-1.5 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
          <label className="text-sm font-medium text-slate-600 ml-1">Hasta:</label>
          <input 
            type="date" 
            className="border border-slate-200 rounded-lg px-2 py-1.5 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
          />
          {(startDate || endDate) && (
            <button onClick={() => { setStartDate(''); setEndDate(''); }} className="text-xs text-slate-500 hover:text-slate-800 underline ml-1">
              Limpiar
            </button>
          )}
        </div>
      </div>

      {/* Grid de tarjetas de métricas */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        <MetricCard 
          title="Ventas de Hoy" 
          value={`$` + (summary?.todaySales?.toLocaleString('es-AR', {minimumFractionDigits: 2}) || '0.00')} 
          icon={<DollarSign className="text-emerald-600 h-6 w-6" />} 
          bgClass="bg-emerald-50"
          trend="+12% vs ayer"
          trendUp={true}
        />
        <MetricCard 
          title="Ventas Semanales" 
          value={`$` + (summary?.weeklySales?.toLocaleString('es-AR', {minimumFractionDigits: 2}) || '0.00')} 
          icon={<TrendingUp className="text-blue-600 h-6 w-6" />} 
          bgClass="bg-blue-50"
        />
        <MetricCard 
          title="Ventas Mensuales" 
          value={`$` + (summary?.monthlySales?.toLocaleString('es-AR', {minimumFractionDigits: 2}) || '0.00')} 
          icon={<TrendingUp className="text-indigo-600 h-6 w-6" />} 
          bgClass="bg-indigo-50"
        />
        <MetricCard 
          title="Total Productos" 
          value={summary?.totalProducts || '0'} 
          icon={<Package className="text-purple-600 h-6 w-6" />} 
          bgClass="bg-purple-50"
        />
      </div>

      {/* Gráfico de Evolución de Ingresos */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 mt-8">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-xl font-bold text-slate-800 flex items-center gap-2">
            <BarChart3 className="text-indigo-500 h-6 w-6" />
            Evolución de Ingresos por Medio de Pago
          </h3>
        </div>
        
        {chartData.length > 0 ? (
          <div className="h-80 w-full">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                <XAxis dataKey="date" axisLine={false} tickLine={false} tick={{fill: '#64748b'}} />
                <YAxis axisLine={false} tickLine={false} tick={{fill: '#64748b'}} tickFormatter={(value) => `$` + value} />
                <Tooltip 
                  formatter={(value: any) => [`$` + Number(value).toLocaleString('es-AR', {minimumFractionDigits: 2}), undefined]}
                  contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                />
                <Legend iconType="circle" />
                {paymentMethods.map((method, index) => (
                  <Bar key={method as string} dataKey={method as string} name={method as string} stackId="a" fill={colors[index % colors.length]} radius={[4, 4, 0, 0]} />
                ))}
              </BarChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <div className="h-72 w-full bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-center border-dashed">
            <span className="text-sm font-medium text-slate-400">No hay datos de ventas recientes para graficar en estas fechas.</span>
          </div>
        )}
      </div>

      {/* Tablero de Control de Cajas POS */}
      {posBoxes && posBoxes.length > 0 && (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 mt-8">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-xl font-bold text-slate-800 flex items-center gap-2">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
              Tablero de Control de Cajas POS
            </h3>
          </div>
          
          {/* Resumen Global */}
          <div className="flex flex-col md:flex-row bg-[#1a1c30] rounded-xl overflow-hidden shadow-sm border border-slate-700/50 border-l-[4px] border-l-emerald-500 mb-6 text-white p-5 gap-6">
            <div className="flex-1 min-w-[250px]">
              <h4 className="font-bold flex items-center gap-2 mb-4 text-[15px]">
                <TrendingUp className="w-5 h-5 text-white" />
                Resumen Global (Cajas Abiertas: {globalPosStats.cajasAbiertas})
              </h4>
              
              <div className="flex flex-wrap gap-4">
                <div className="flex-1 bg-blue-500/10 rounded-lg p-3 border border-blue-500/20 min-w-[140px]">
                  <p className="text-[10px] text-blue-300 font-bold uppercase tracking-wider mb-1">Total Ventas Global</p>
                  <p className="text-2xl font-bold text-blue-400">${globalPosStats.totalVentas.toLocaleString('es-AR', {minimumFractionDigits: 2})}</p>
                </div>
                
                <div className="flex-1 bg-emerald-500/10 rounded-lg p-3 border border-emerald-500/20 min-w-[140px]">
                  <p className="text-[10px] text-emerald-400 font-bold uppercase tracking-wider mb-1">Efectivo Global Esperado</p>
                  <p className="text-2xl font-bold text-emerald-500">${globalPosStats.totalEfectivoEsperado.toLocaleString('es-AR', {minimumFractionDigits: 2})}</p>
                </div>
              </div>
            </div>
            
            <div className="flex-[1.2] min-w-[300px] bg-slate-800/30 rounded-lg p-4 border border-slate-700/30">
              <h5 className="text-[11px] font-bold text-slate-300 border-b border-dashed border-slate-600/50 pb-2 mb-3">Desglose Global por Medios de Pago</h5>
              {globalPosStats.desglose.length > 0 ? (
                <div className="grid grid-cols-2 gap-2">
                  {globalPosStats.desglose.map(pb => (
                    <div key={pb.method} className="flex justify-between items-center text-xs bg-black/20 px-3 py-2 rounded-md">
                      <span className="text-slate-300">{pb.method}</span>
                      <span className="font-bold text-slate-100">${pb.amount.toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-xs text-slate-400 italic text-center py-4">Sin pagos registrados en cajas abiertas</div>
              )}
            </div>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {posBoxes.map((pos: any) => (
              <PosCard key={pos.id} pos={pos} />
            ))}
          </div>
        </div>
      )}

      {/* Historial de Cajas Cerradas */}
      {posHistory && posHistory.length > 0 && (
        <PosHistorySection history={posHistory} startDate={startDate} endDate={endDate} />
      )}
      
      {/* Top Productos Vendidos */}
      {topProducts && topProducts.length > 0 && (
        <TopProductsSection products={topProducts} />
      )}
      
    </div>
  );
}

function MetricCard({ title, value, icon, bgClass, trend, trendUp }: any) {
  return (
    <div className="relative overflow-hidden rounded-2xl bg-white shadow-sm border border-slate-100 p-6 transition-all hover:shadow-md hover:-translate-y-1">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500 mb-1">{title}</p>
          <p className="text-3xl font-bold text-slate-900 tracking-tight">{value}</p>
        </div>
        <div className={`flex items-center justify-center p-3 rounded-xl ${bgClass}`}>
          {icon}
        </div>
      </div>
      {trend && (
        <div className="mt-4 flex items-center text-sm">
          <span className={`font-medium ${trendUp ? 'text-emerald-600' : 'text-rose-600'}`}>
            {trend}
          </span>
        </div>
      )}
    </div>
  );
}

function PosCard({ pos }: { pos: any }) {
  const [showModal, setShowModal] = useState(false);
  const isOpen = pos.isOpen;
  // eslint-disable-next-line react-hooks/purity
      const isUpdated = pos.lastSyncDate && new Date(pos.lastSyncDate).getTime() > Date.now() - 24 * 60 * 60 * 1000;
  
    return (
      <>
    <div className="flex flex-col bg-[#1E213A] rounded-2xl overflow-hidden shadow-md text-white border border-slate-700/50">
      <div className="p-5 pb-3">
        <div className="flex justify-between items-center mb-4">
          <h4 className="text-lg font-bold tracking-tight">{pos.name}</h4>
          <span className={`px-2.5 py-1 text-xs font-bold rounded-full ${isOpen ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30' : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'}`}>
            {isOpen ? '● ABIERTA' : '● CERRADA'}
          </span>
        </div>
        
        <div className={`flex items-center gap-2 text-xs font-medium px-3 py-2 rounded-md mb-4 border ${isUpdated ? 'bg-[#0f2e24] text-[#10b981] border-[#10b981]/30' : 'bg-[#312513] text-[#f59e0b] border-[#f59e0b]/30'}`}>
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
          Catálogo: {isUpdated ? 'Actualizado' : 'Desactualizado'}
          {pos.lastSyncDate && <span className="ml-auto opacity-75 text-[10px]">{new Date(pos.lastSyncDate).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>}
        </div>

        {isOpen ? (
          <div className="space-y-4 text-sm text-slate-300">
            <div className="space-y-1">
              <div className="flex items-center gap-2 text-slate-400 text-xs">
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
                <span className="truncate">Cajero: <span className="text-slate-200 font-medium">{pos.cashier || 'N/A'}</span></span>
              </div>
              <div className="flex items-center gap-2 text-slate-400 text-xs">
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                <span className="truncate">Apertura: <span className="text-slate-200 font-medium">{pos.openingDate ? new Date(pos.openingDate).toLocaleDateString('es-AR', {day: '2-digit', month: '2-digit', year: '2-digit'}) + ' ' + new Date(pos.openingDate).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) : 'N/A'}</span></span>
              </div>
            </div>

                          <div className="bg-emerald-500/10 rounded-xl p-4 border border-emerald-500/20 mb-3">
                <p className="text-xs text-emerald-400 font-medium tracking-wide mb-1">Total Efectivo en Caja</p>
                <p className="text-2xl font-bold text-emerald-500">${(pos.cashBalance || 0).toLocaleString('es-AR', {minimumFractionDigits: 2})}</p>
                                <p className="text-[11px] text-emerald-400/70 mt-1">(Efectivo Inicial: ${(pos.initialBalance || 0).toLocaleString('es-AR', {minimumFractionDigits: 2})})</p>
                {(pos.cashIn > 0 || pos.cashOut > 0) && (
                  <div className="mt-2 pt-2 border-t border-dashed border-emerald-500/30 text-xs text-emerald-400/80 space-y-1">
                    {pos.cashIn > 0 && (
                      <div className="flex justify-between">
                        <span>Entradas / Ingresos:</span>
                        <span className="font-bold">+${(pos.cashIn).toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                      </div>
                    )}
                    {pos.cashOut > 0 && (
                      <div className="flex justify-between text-red-400">
                        <span>Retiros / Egresos:</span>
                        <span className="font-bold">-${(pos.cashOut).toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                      </div>
                    )}
                  </div>
                )}
              </div>

              <div className="bg-blue-500/10 rounded-xl p-4 border border-blue-500/20">
                <p className="text-xs text-blue-400 font-bold tracking-wide">Desglose de Ventas ({pos.salesCount || 0}):</p>
                
                <div className="my-3 py-2 border-y border-dashed border-blue-500/30 space-y-1.5">
                  {pos.paymentBreakdown && pos.paymentBreakdown.length > 0 ? (
                    pos.paymentBreakdown.map((pb: any) => (
                      <div key={pb.method} className="flex justify-between items-center text-xs text-blue-300">
                        <span>{pb.method}</span>
                        <span className="font-bold">${(pb.amount || 0).toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                      </div>
                    ))
                  ) : (
                    <div className="text-xs text-blue-300/50 italic">Sin pagos registrados</div>
                  )}
                </div>
                
                <div className="flex justify-between items-baseline mt-2">
                  <span className="text-xs text-blue-400 font-bold">TOTAL VENTAS:</span>
                  <span className="text-xl font-bold text-blue-400">${(pos.salesAmount || 0).toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                  </div>
                  {pos.cancelledSalesCount > 0 && (
                    <div className="flex justify-between items-baseline mt-2 pt-2 border-t border-rose-500/20">
                      <span className="text-xs text-rose-500 font-bold">ANULADAS ({pos.cancelledSalesCount}):</span>
                      <span className="text-sm font-bold text-rose-500">${(pos.cancelledSalesAmount || 0).toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                    </div>
                  )}
              </div>
            </div>
          ) : (
          <div className="py-8 flex flex-col items-center justify-center text-slate-500">
            <svg className="w-12 h-12 mb-2 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" /></svg>              <span className="text-sm">No hay caja activa</span>
            </div>
          )}
        </div>
        <div className="p-5 pt-0 mt-auto">          <button onClick={() => setShowModal(true)} className="w-full bg-[#8B5CF6] hover:bg-[#7C3AED] text-white font-bold py-2.5 px-4 rounded-xl flex items-center justify-center gap-2 transition-colors shadow-[0_0_15px_rgba(139,92,246,0.2)]">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
            Ver Detalles y Reportes
          </button>
        </div>
      </div>
      
      {showModal && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-in fade-in duration-200" style={{ margin: 0 }}>
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[85vh] overflow-hidden flex flex-col">
            <div className="p-5 border-b border-slate-100 flex justify-between items-center bg-slate-50/80">
              <div>
                <h3 className="font-bold text-slate-800 text-lg flex items-center gap-2">
                  <svg className="w-5 h-5 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                  Reporte de Caja: {pos.name}
                </h3>
                <p className="text-xs text-slate-500 mt-1">Cajero: {pos.cashier || 'N/A'}</p>
              </div>
              <button onClick={() => setShowModal(false)} className="bg-white text-slate-400 hover:text-rose-500 p-2 rounded-full shadow-sm border border-slate-100 hover:bg-rose-50 transition-colors">
                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            
            <div className="p-6 overflow-y-auto bg-slate-50/30">
              <h4 className="font-bold text-slate-700 mb-3 text-xs uppercase tracking-widest flex items-center gap-2">
                <svg className="w-4 h-4 text-emerald-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                Desglose de Pagos
              </h4>
              {pos.paymentBreakdown && pos.paymentBreakdown.length > 0 ? (
                <div className="space-y-2 mb-8">
                  {pos.paymentBreakdown.map((p: any) => (
                    <div key={p.method} className="flex justify-between items-center bg-white p-3.5 rounded-xl border border-slate-200 shadow-sm">
                      <span className="font-medium text-slate-600">{p.method}</span>
                      <span className="font-bold text-emerald-600 text-lg">${p.amount?.toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="bg-white p-4 rounded-xl border border-dashed border-slate-200 text-center mb-8">
                  <p className="text-sm text-slate-400 italic">No hay pagos registrados.</p>
                </div>
              )}

              <h4 className="font-bold text-slate-700 mb-3 text-xs uppercase tracking-widest flex items-center gap-2">
                <svg className="w-4 h-4 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" /></svg>
                Últimas Ventas (Top 15)
              </h4>
              {pos.recentSales && pos.recentSales.length > 0 ? (
                <div className="space-y-2">
                  {pos.recentSales.map((s: any) => (
                    <div key={s.id} className={`flex justify-between items-center text-sm p-3.5 rounded-xl border ${s.isCancelled ? 'border-rose-200 bg-rose-50/50 opacity-75' : 'border-slate-200 bg-white hover:bg-slate-50 transition-colors shadow-sm'}`}>
                        <div>
                          <span className={`font-bold block ${s.isCancelled ? 'text-rose-700' : 'text-slate-700'}`}>
                            Ticket {s.posNumber ? s.posNumber.toString().padStart(5, '0') + '-' : '#'}{s.id.toString().padStart(8, '0')}
                            {s.isCancelled && <span className="ml-2 bg-rose-100 text-rose-600 px-1.5 py-0.5 rounded text-[10px]">ANULADA</span>}
                          </span>
                          <span className={`text-xs ${s.isCancelled ? 'text-rose-500/70' : 'text-slate-500'}`}>{new Date(s.date).toLocaleString('es-AR', {dateStyle:'short', timeStyle:'short'})}</span>
                        </div>
                        <span className={`font-bold text-base ${s.isCancelled ? 'text-rose-600 line-through' : 'text-slate-800'}`}>${s.totalAmount?.toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                      </div>
                  ))}
                </div>
              ) : (
                <div className="bg-white p-4 rounded-xl border border-dashed border-slate-200 text-center">
                  <p className="text-sm text-slate-400 italic">No hay ventas recientes.</p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function PosHistorySection({ history, startDate, endDate }: { history: any[], startDate: string, endDate: string }) {
  const filteredHistory = history.filter(h => {
    if (!h.closingDate) return true;
    const dateStr = h.closingDate.split('T')[0];
    if (startDate && dateStr < startDate) return false;
    if (endDate && dateStr > endDate) return false;
    return true;
  });

  const totalSalesAmount = filteredHistory.reduce((sum, h) => sum + (h.salesAmount || 0), 0);
  const totalTickets = filteredHistory.reduce((sum, h) => sum + (h.salesCount || 0), 0);

  return (
    <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 mt-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-6 gap-4">
        <h3 className="text-xl font-bold text-slate-800 flex items-center gap-2">
          <svg className="h-6 w-6 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          Historial de Cajas Cerradas
        </h3>
      </div>
      
      {/* TARJETA DE TOTAL DE CAJAS FILTRADAS */}
      <div className="mb-6 bg-indigo-50 border border-indigo-100 rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
           <p className="text-indigo-900 font-medium text-sm">
             {(startDate || endDate) ? 'Resumen del período seleccionado' : 'Resumen global del historial (Últimos 30 días)'}
           </p>
           <p className="text-indigo-700 text-xs mt-1">Cajas mostradas: {filteredHistory.length} | Tickets emitidos: {totalTickets}</p>
        </div>
        <div className="text-right">
           <p className="text-xs text-indigo-700 font-medium uppercase tracking-wider mb-1">Total de Ventas</p>
           <p className="text-3xl font-black text-indigo-900">
             ${totalSalesAmount.toLocaleString('es-AR', {minimumFractionDigits: 2})}
           </p>
        </div>
      </div>

      {filteredHistory.length === 0 ? (
        <div className="text-center py-10 text-slate-500 bg-slate-50 rounded-xl border border-dashed border-slate-200">
          No hay cajas cerradas en la fecha seleccionada.
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left text-slate-600 whitespace-nowrap">
            <thead className="text-xs text-slate-500 uppercase bg-slate-50 border-b border-slate-100">
              <tr>
                <th className="px-4 py-3 font-medium">Caja</th>
                <th className="px-4 py-3 font-medium">Cajero</th>
                <th className="px-4 py-3 font-medium">Apertura / Cierre</th>
                <th className="px-4 py-3 font-medium">Inicio</th>
                <th className="px-4 py-3 font-medium">Total Ventas</th>
                <th className="px-4 py-3 font-medium">Medios de Pago</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredHistory.map((h: any) => (
                <tr key={h.id} className="hover:bg-slate-50/50">
                  <td className="px-4 py-4 font-bold text-slate-800">{h.posName}</td>
                  <td className="px-4 py-4">{h.cashier || '---'}</td>
                  <td className="px-4 py-4">
                    <div className="flex flex-col">
                      <span className="text-emerald-600 text-xs">Ap: {new Date(h.openingDate).toLocaleString('es-AR', {dateStyle:'short', timeStyle:'short'})}</span>
                      <span className="text-rose-600 text-xs">Ci: {h.closingDate ? new Date(h.closingDate).toLocaleString('es-AR', {dateStyle:'short', timeStyle:'short'}) : '---'}</span>
                    </div>
                  </td>
                  <td className="px-4 py-4 font-medium">${h.initialBalance?.toLocaleString('es-AR', {minimumFractionDigits: 2}) || '0.00'}</td>
                  <td className="px-4 py-4">
                    <span className="font-bold text-slate-800">${h.salesAmount?.toLocaleString('es-AR', {minimumFractionDigits: 2}) || '0.00'}</span>
                    <span className="text-xs text-slate-400 block">({h.salesCount} tickets)</span>
                  </td>
                  <td className="px-4 py-4 text-xs">
                    {h.paymentBreakdown && h.paymentBreakdown.length > 0 ? (
                      <div className="space-y-1">
                        {h.paymentBreakdown.map((pb: any) => (
                          <div key={pb.method} className="flex justify-between gap-4">
                            <span className="text-slate-500">{pb.method}:</span>
                            <span className="font-medium text-slate-700">${pb.amount?.toLocaleString('es-AR', {minimumFractionDigits: 2})}</span>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <span className="text-slate-400 italic">Sin detalle</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function TopProductsSection({ products }: { products: any[] }) {
  return (
    <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 mt-8">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-xl font-bold text-slate-800 flex items-center gap-2">
          <Award className="text-amber-500 h-6 w-6" />
          Top Artículos Más Vendidos
        </h3>
      </div>
      
      <div className="overflow-x-auto">
        <table className="w-full text-sm text-left text-slate-600 whitespace-nowrap">
          <thead className="text-xs text-slate-500 uppercase bg-slate-50 border-b border-slate-100">
            <tr>
              <th className="px-4 py-3 font-medium w-16">#</th>
              <th className="px-4 py-3 font-medium">Código</th>
              <th className="px-4 py-3 font-medium">Artículo</th>
              <th className="px-4 py-3 font-medium text-right">Cant. Vendida</th>
              <th className="px-4 py-3 font-medium text-right">Recaudación</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {products.map((p: any, idx: number) => (
              <tr key={p.internalCode || idx} className="hover:bg-slate-50/50">
                <td className="px-4 py-4 font-bold text-slate-400">
                  {idx === 0 ? <span className="text-amber-500 text-lg">🥇</span> : 
                   idx === 1 ? <span className="text-slate-400 text-lg">🥈</span> : 
                   idx === 2 ? <span className="text-amber-700 text-lg">🥉</span> : 
                   (idx + 1)}
                </td>
                <td className="px-4 py-4 text-slate-500 font-mono">{p.internalCode || 'S/C'}</td>
                <td className="px-4 py-4 font-medium text-slate-800">{p.name}</td>
                <td className="px-4 py-4 text-right">
                  <span className="bg-slate-100 text-slate-700 px-2 py-1 rounded font-bold">{p.totalQuantity}</span>
                </td>
                <td className="px-4 py-4 text-right font-bold text-emerald-600">
                  ${p.totalRevenue?.toLocaleString('es-AR', {minimumFractionDigits: 2})}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}








