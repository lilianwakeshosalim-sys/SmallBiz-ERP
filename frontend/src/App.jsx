import { useEffect, useState } from "react";
import InventoryPage from "./InventoryPage";
import "./App.css";
import { getDashboard } from "./api";

const emptyStats = {
  totalSales: 0,
  totalRevenue: 0,
  totalPaymentsReceived: 0,
  outstandingSalesBalance: 0,
  totalProducts: 0,
  lowStockProducts: 0,
};

const money = (value) =>
  new Intl.NumberFormat("en-KE", {
    style: "currency",
    currency: "KES",
    maximumFractionDigits: 0,
  }).format(Number(value || 0));

function Icon({ name, size = 20 }) {
  const paths = {
    dashboard: <><rect x="3" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="14" width="7" height="7" rx="1.5" /><rect x="3" y="14" width="7" height="7" rx="1.5" /></>,
    inventory: <><path d="m12 3 9 5-9 5-9-5 9-5Z" /><path d="m3 12 9 5 9-5M3 16l9 5 9-5" /></>,
    sales: <><path d="M3 3v18h18" /><path d="m7 14 4-4 4 3 6-7" /><path d="M16 6h5v5" /></>,
    purchases: <><path d="M12 3v12m-5-5 5 5 5-5" /><path d="M5 17v4h14v-4" /></>,
    customers: <><path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="10" cy="7" r="4" /><path d="M20 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" /></>,
    suppliers: <><path d="M3 21h18M5 21V7l7-4 7 4v14M9 21v-6h6v6" /><path d="M9 9h.01M15 9h.01" /></>,
    expenses: <><rect x="3" y="5" width="18" height="15" rx="2" /><path d="M3 10h18M7 15h3" /><path d="M7 5V3h10v2" /></>,
    reports: <><path d="M4 19V5M4 19h17" /><path d="M8 15v-4m5 4V7m5 8v-6" /></>,
    search: <><circle cx="11" cy="11" r="7" /><path d="m20 20-4-4" /></>,
    bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" /><path d="M10 21h4" /></>,
    refresh: <><path d="M20 7v5h-5" /><path d="M4 17v-5h5" /><path d="M5.6 9A7 7 0 0 1 17 5l3 2M4 17l3 2a7 7 0 0 0 11.4-4" /></>,
    arrow: <><path d="M7 17 17 7M7 7h10v10" /></>,
    wallet: <><rect x="3" y="5" width="18" height="15" rx="2" /><path d="M3 9h18M16 15h.01" /><path d="M6 5V3h12v2" /></>,
    package: <><path d="m12 3 9 5v8l-9 5-9-5V8l9-5Z" /><path d="m3 8 9 5 9-5M12 13v8M7.5 5.5l9 5" /></>,
    check: <><circle cx="12" cy="12" r="9" /><path d="m8 12 3 3 5-6" /></>,
    clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>,
    plus: <><path d="M12 5v14M5 12h14" /></>,
    menu: <><path d="M4 6h16M4 12h16M4 18h16" /></>,
  };

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {paths[name] || paths.dashboard}
    </svg>
  );
}

function App() {
  const [stats, setStats] = useState(emptyStats);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [lastUpdated, setLastUpdated] = useState(null);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [activeNav, setActiveNav] = useState("Dashboard");

  async function loadDashboard() {
    setLoading(true);
    setError("");

    try {
      const data = await getDashboard();
      setStats({ ...emptyStats, ...data });
      setLastUpdated(new Date());
    } catch (err) {
      console.error("Dashboard request failed:", err);
      setError(
        "We couldn't retrieve the latest figures. Check that the API is running and try again."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadDashboard();
  }, []);

  const revenue = Number(stats.totalRevenue || 0);
  const payments = Number(stats.totalPaymentsReceived || 0);
  const outstanding = Number(stats.outstandingSalesBalance || 0);
  const products = Number(stats.totalProducts || 0);
  const lowStock = Number(stats.lowStockProducts || 0);
  const sales = Number(stats.totalSales || 0);

  const financialMax = Math.max(revenue, payments, outstanding, 1);
  const inventoryMax = Math.max(products, lowStock, 1);
  const healthyProducts = Math.max(products - lowStock, 0);
  const stockHealth = products > 0
    ? Math.max(0, Math.min(100, (healthyProducts / products) * 100))
    : 0;

  const metrics = [
    {
      label: "Total revenue",
      value: money(revenue),
      detail: "Recorded sales value",
      icon: "sales",
      tone: "emerald",
    },
    {
      label: "Payments received",
      value: money(payments),
      detail: "Collected from sales",
      icon: "wallet",
      tone: "blue",
    },
    {
      label: "Products",
      value: products.toLocaleString("en-KE"),
      detail: "Registered inventory items",
      icon: "package",
      tone: "violet",
    },
    {
      label: "Low-stock alerts",
      value: lowStock.toLocaleString("en-KE"),
      detail: "Products to review",
      icon: "inventory",
      tone: "amber",
    },
    {
      label: "Outstanding balance",
      value: money(outstanding),
      detail: "Unpaid sales balance",
      icon: "clock",
      tone: "rose",
    },
    {
      label: "Completed sales",
      value: sales.toLocaleString("en-KE"),
      detail: "Sales transactions",
      icon: "check",
      tone: "teal",
    },
  ];

  const navItems = [
    { label: "Dashboard", icon: "dashboard" },
    { label: "Inventory", icon: "inventory" },
    { label: "Sales", icon: "sales" },
    { label: "Purchases", icon: "purchases" },
    { label: "Customers", icon: "customers" },
    { label: "Suppliers", icon: "suppliers" },
    { label: "Expenses", icon: "expenses" },
    { label: "Reports", icon: "reports" },
  ];

  function chooseNav(label) {
    setActiveNav(label);
    setMobileMenuOpen(false);

    if (label !== "Dashboard" && label !== "Inventory") {
      alert(
        `${label} module is not connected to the dashboard yet. We'll build it next.`
      );
    }
  }

  return (
    <div className="app-shell">
      <aside className={`sidebar ${mobileMenuOpen ? "sidebar-open" : ""}`}>
        <a className="brand" href="#dashboard" onClick={() => chooseNav("Dashboard")}>
          <span className="brand-mark">S<span>.</span></span>
          <span className="brand-copy">
            <strong>SmallBiz<span> ERP</span></strong>
            <small>BUSINESS MANAGEMENT</small>
          </span>
        </a>

        <div className="business-switcher">
          <div className="business-monogram">L</div>
          <div className="business-copy">
            <small>YOUR BUSINESS</small>
            <strong>LilyTech Electronics</strong>
            <span><i className="online-dot" /> Nairobi, Kenya</span>
          </div>
          <span className="switcher-arrow">⌄</span>
        </div>

        <div className="nav-caption">WORKSPACE</div>
        <nav className="side-nav" aria-label="Main navigation">
          {navItems.map((item) => (
            <button
              key={item.label}
              className={`nav-link ${activeNav === item.label ? "active" : ""}`}
              onClick={() => chooseNav(item.label)}
            >
              <Icon name={item.icon} size={18} />
              <span>{item.label}</span>
              {item.label === "Dashboard" && <span className="active-indicator" />}
            </button>
          ))}
        </nav>

        <div className="sidebar-note">
          <div className="note-icon"><Icon name="reports" size={19} /></div>
          <strong>Grow with clarity.</strong>
          <p>Your business information, all in one place.</p>
        </div>

        <div className="profile-row">
          <div className="profile-avatar">L</div>
          <div className="profile-info">
            <strong>Lily</strong>
            <span>Administrator</span>
          </div>
          <span className="profile-menu">•••</span>
        </div>
      </aside>

      {mobileMenuOpen && (
        <button
          className="mobile-backdrop"
          aria-label="Close navigation"
          onClick={() => setMobileMenuOpen(false)}
        />
      )}

      <main className="main-area" id="dashboard">
        <header className="topbar">
          <div className="topbar-left">
            <button
              className="mobile-menu-button"
              aria-label="Open navigation"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              <Icon name="menu" size={21} />
            </button>
            <div className="breadcrumb">
              <span>Workspace</span><b>/</b><strong>{activeNav}</strong>
            </div>
          </div>
          <div className="topbar-actions">
            <div className="header-status"><i className="online-dot" /> Business overview</div>
            <button className="icon-button notification-button" aria-label="Notifications" onClick={() => alert("Notifications will be available as we build the next modules.")}>
              <Icon name="bell" size={19} />
              <span />
            </button>
            <div className="header-avatar">L</div>
          </div>
        </header>

        {activeNav === "Inventory" ? <InventoryPage /> : (
        <div className="page-content">
          <section className="welcome-banner">
            <div className="welcome-content">
              <div className="eyebrow"><span /> YOUR BUSINESS AT A GLANCE</div>
              <h1>Good to see you, Lily<span>.</span></h1>
              <p>Here's how your business is doing. Let's make today count.</p>
              <div className="welcome-meta">
                <span className="meta-check"><Icon name="check" size={14} /></span>
                <span>{lastUpdated ? `Data updated at ${lastUpdated.toLocaleTimeString("en-KE", { hour: "2-digit", minute: "2-digit" })}` : "Your business dashboard"}</span>
              </div>
            </div>
            <div className="banner-art" aria-hidden="true">
              <div className="art-ring art-ring-one" />
              <div className="art-ring art-ring-two" />
              <div className="art-square"><Icon name="reports" size={39} /></div>
              <div className="art-sparkle">✳</div>
              <div className="art-dot" />
            </div>
          </section>

          {error && (
            <div className="error-banner" role="alert">
              <div><strong>Unable to refresh dashboard</strong><p>{error}</p></div>
              <button onClick={loadDashboard}>Try again</button>
            </div>
          )}

          <div className="section-title-row">
            <div>
              <div className="section-kicker">OVERVIEW</div>
              <h2>Business performance</h2>
              <p>Your key numbers, updated from your business database.</p>
            </div>
            <button className="refresh-button" onClick={loadDashboard} disabled={loading}>
              <Icon name="refresh" size={16} />
              {loading ? "Updating..." : "Refresh data"}
            </button>
          </div>

          <section className="metrics-grid" aria-label="Business metrics">
            {metrics.map((metric) => (
              <article className="metric-card" key={metric.label}>
                <div className="metric-top">
                  <span className={`metric-icon ${metric.tone}`}><Icon name={metric.icon} size={19} /></span>
                  <span className="metric-menu">•••</span>
                </div>
                <div className="metric-label">{metric.label}</div>
                <div className="metric-value">{loading && !lastUpdated ? "—" : metric.value}</div>
                <div className="metric-footnote">
                  <span className="metric-footnote-dot" />
                  {metric.detail}
                </div>
              </article>
            ))}
          </section>

          <section className="analytics-grid">
            <article className="content-card financial-card">
              <div className="card-heading">
                <div>
                  <div className="section-kicker">FINANCIAL SNAPSHOT</div>
                  <h2>Money overview</h2>
                  <p>A comparison of the totals currently available from your API.</p>
                </div>
                <span className="card-icon"><Icon name="wallet" size={19} /></span>
              </div>

              <div className="money-summary">
                <div>
                  <span className="summary-label">Total revenue</span>
                  <strong>{loading && !lastUpdated ? "—" : money(revenue)}</strong>
                </div>
                <span className="summary-period">All recorded sales</span>
              </div>

              <div className="bar-chart" role="img" aria-label="Comparison of revenue, payments received and outstanding sales balance">
                {[
                  { label: "Revenue", amount: revenue, color: "bar-emerald" },
                  { label: "Payments received", amount: payments, color: "bar-navy" },
                  { label: "Outstanding", amount: outstanding, color: "bar-gold" },
                ].map((item) => (
                  <div className="bar-row" key={item.label}>
                    <div className="bar-label-row">
                      <span>{item.label}</span>
                      <strong>{money(item.amount)}</strong>
                    </div>
                    <div className="bar-track">
                      <div
                        className={`bar-fill ${item.color}`}
                        style={{ width: `${Math.max(0, Math.min(100, (item.amount / financialMax) * 100))}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
              <div className="chart-footnote">
                <span className="footnote-mark">i</span>
                These are current totals, not a historical time series.
              </div>
            </article>

            <article className="content-card inventory-card">
              <div className="card-heading">
                <div>
                  <div className="section-kicker">INVENTORY HEALTH</div>
                  <h2>Stock at a glance</h2>
                  <p>Based on the product and low-stock totals returned by the API.</p>
                </div>
                <span className="card-icon inventory-card-icon"><Icon name="package" size={19} /></span>
              </div>

              <div className="inventory-total">
                <div className="inventory-total-number">{loading && !lastUpdated ? "—" : products.toLocaleString("en-KE")}</div>
                <div className="inventory-total-label">Registered products</div>
              </div>

              <div className="inventory-progress" aria-label={`Inventory overview: ${healthyProducts} products not flagged as low stock, ${lowStock} low-stock products`}>
                <div
                  className="inventory-progress-healthy"
                  style={{ width: `${products > 0 ? (healthyProducts / products) * 100 : 0}%` }}
                />
                <div
                  className="inventory-progress-low"
                  style={{ width: `${products > 0 ? (Math.min(lowStock, products) / products) * 100 : 0}%` }}
                />
              </div>

              <div className="inventory-legend">
                <div className="legend-row">
                  <span className="legend-dot healthy-dot" />
                  <span>Not flagged as low stock</span>
                  <strong>{healthyProducts}</strong>
                </div>
                <div className="legend-row">
                  <span className="legend-dot low-dot" />
                  <span>Low-stock alerts</span>
                  <strong>{lowStock}</strong>
                </div>
              </div>

              <div className={`stock-status ${lowStock > 0 ? "stock-warning" : "stock-good"}`}>
                <Icon name={lowStock > 0 ? "clock" : "check"} size={17} />
                <div>
                  <strong>{lowStock > 0 ? "Stock needs attention" : "No low-stock alerts"}</strong>
                  <span>{lowStock > 0 ? "Review products flagged by the inventory system." : products > 0 ? "No products are currently flagged as low stock." : "Add products to start tracking inventory."}</span>
                </div>
              </div>
              <div className="chart-footnote">
                <span className="footnote-mark">i</span>
                This is not a stock quantity breakdown.
              </div>
            </article>
          </section>

          <section className="bottom-grid">
            <article className="content-card activity-card">
              <div className="card-heading">
                <div>
                  <div className="section-kicker">QUICK SUMMARY</div>
                  <h2>Business at a glance</h2>
                </div>
                <span className="live-badge"><i /> API connected</span>
              </div>
              <div className="summary-list">
                <div className="summary-row">
                  <span className="summary-row-icon emerald-soft"><Icon name="sales" size={18} /></span>
                  <div><strong>Sales transactions</strong><span>Completed sales returned by your API</span></div>
                  <b>{sales}</b>
                </div>
                <div className="summary-row">
                  <span className="summary-row-icon blue-soft"><Icon name="wallet" size={18} /></span>
                  <div><strong>Payments collected</strong><span>Recorded payment total</span></div>
                  <b>{money(payments)}</b>
                </div>
                <div className="summary-row">
                  <span className="summary-row-icon amber-soft"><Icon name="clock" size={18} /></span>
                  <div><strong>Unpaid sales balance</strong><span>Outstanding balance reported by the API</span></div>
                  <b>{money(outstanding)}</b>
                </div>
              </div>
            </article>

            <article className="content-card actions-card">
              <div className="card-heading">
                <div>
                  <div className="section-kicker">SHORTCUTS</div>
                  <h2>Quick actions</h2>
                  <p>Get to your daily tasks.</p>
                </div>
              </div>
              <button className="action-link" onClick={() => chooseNav("Inventory")}>
                <span className="action-link-icon"><Icon name="plus" size={18} /></span>
                <span><strong>Add a product</strong><small>Register an inventory item</small></span>
                <b>›</b>
              </button>
              <button className="action-link" onClick={() => chooseNav("Sales")}>
                <span className="action-link-icon"><Icon name="sales" size={18} /></span>
                <span><strong>Record a sale</strong><small>Record a customer purchase</small></span>
                <b>›</b>
              </button>
              <button className="action-link" onClick={() => chooseNav("Customers")}>
                <span className="action-link-icon"><Icon name="customers" size={18} /></span>
                <span><strong>Add a customer</strong><small>Manage customer details</small></span>
                <b>›</b>
              </button>
            </article>
          </section>

          <footer className="page-footer">
            <span>© 2026 <strong>SmallBiz ERP</strong></span>
            <span>Made for growing businesses <span className="footer-heart">♥</span></span>
          </footer>
        </div>
        )}
      </main>
    </div>
  );
}

export default App;



