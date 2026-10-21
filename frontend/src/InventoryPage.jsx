import { useEffect, useMemo, useState } from "react";
import { getInventory, getCategories, createProduct, adjustStock } from "./api";
import "./InventoryPage.css";

const EMPTY_FORM = {
  name: "",
  sku: "",
  categoryId: "",
  description: "",
  buyingPrice: "",
  sellingPrice: "",
  minimumStockLevel: "5",
  openingStock: "0",
};

const EMPTY_ADJUSTMENT = {
  productId: "",
  action: "Receive",
  quantity: "1",
  physicalCount: "",
  unitCost: "",
  referenceNumber: "",
  notes: "",
};

function formatMoney(value) {
  return new Intl.NumberFormat("en-KE", {
    style: "currency",
    currency: "KES",
    maximumFractionDigits: 0,
  }).format(Number(value) || 0);
}

export default function InventoryPage() {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("all");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [showAdjustment, setShowAdjustment] = useState(false);
  const [adjustment, setAdjustment] = useState(EMPTY_ADJUSTMENT);
  const [adjustmentError, setAdjustmentError] = useState("");
  const [adjustmentSuccess, setAdjustmentSuccess] = useState("");
  const [adjusting, setAdjusting] = useState(false);

  async function loadInventory() {
    setLoading(true);
    setError("");
    try {
      const [productData, categoryData] = await Promise.all([
        getInventory(),
        getCategories(),
      ]);
      setProducts(Array.isArray(productData) ? productData : []);
      setCategories(Array.isArray(categoryData) ? categoryData : []);
    } catch (err) {
      setError(err.message || "Could not load inventory.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadInventory();
  }, []);

  function updateForm(event) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  }

  function updateAdjustment(event) {
    const { name, value } = event.target;
    setAdjustment((current) => ({ ...current, [name]: value }));
  }

  async function handleCreateProduct(event) {
    event.preventDefault();
    setFormError("");
    setSuccessMessage("");

    const buyingPrice = Number(form.buyingPrice);
    const sellingPrice = Number(form.sellingPrice);
    const minimumStockLevel = Number(form.minimumStockLevel);
    const openingStock = Number(form.openingStock);

    if (!form.name.trim() || !form.sku.trim() || !form.categoryId) {
      setFormError("Enter the product name and SKU, and select a category.");
      return;
    }

    if (
      form.buyingPrice === "" ||
      form.sellingPrice === "" ||
      !Number.isFinite(buyingPrice) ||
      !Number.isFinite(sellingPrice) ||
      buyingPrice < 0 ||
      sellingPrice < 0
    ) {
      setFormError("Enter valid, non-negative buying and selling prices.");
      return;
    }

    if (
      !Number.isInteger(minimumStockLevel) ||
      minimumStockLevel < 0 ||
      !Number.isInteger(openingStock) ||
      openingStock < 0
    ) {
      setFormError("Minimum stock and opening stock must be whole numbers of zero or more.");
      return;
    }

    setSaving(true);

    try {
      await createProduct({
        name: form.name.trim(),
        sku: form.sku.trim(),
        categoryId: form.categoryId,
        description: form.description.trim() || null,
        buyingPrice,
        sellingPrice,
        minimumStockLevel,
        openingStock,
      });

      setForm(EMPTY_FORM);
      setShowForm(false);
      setSuccessMessage("Product added successfully.");
      await loadInventory();
    } catch (err) {
      setFormError(err.message || "Could not create product. Please try again.");
    } finally {
      setSaving(false);
    }
  }

  async function handleStockAdjustment(event) {
    event.preventDefault();
    setAdjustmentError("");
    setAdjustmentSuccess("");

    if (!adjustment.productId) {
      setAdjustmentError("Select a product.");
      return;
    }

    const selectedProduct = products.find(
      (product) => product.id === adjustment.productId
    );

    if (!selectedProduct) {
      setAdjustmentError("The selected product could not be found. Refresh inventory and try again.");
      return;
    }

    const payload = {
      productId: adjustment.productId,
      action: adjustment.action,
      referenceNumber: adjustment.referenceNumber.trim() || null,
      notes: adjustment.notes.trim() || null,
    };

    if (adjustment.action === "Correction") {
      const physicalCount = Number(adjustment.physicalCount);

      if (
        adjustment.physicalCount === "" ||
        !Number.isInteger(physicalCount) ||
        physicalCount < 0
      ) {
        setAdjustmentError("Enter a physical count that is a whole number of zero or more.");
        return;
      }

      payload.physicalCount = physicalCount;
    } else {
      const quantity = Number(adjustment.quantity);

      if (
        adjustment.quantity === "" ||
        !Number.isInteger(quantity) ||
        quantity <= 0
      ) {
        setAdjustmentError("Quantity must be a whole number greater than zero.");
        return;
      }

      payload.quantity = quantity;

      if (adjustment.action === "Receive" && adjustment.unitCost !== "") {
        const unitCost = Number(adjustment.unitCost);
        if (!Number.isFinite(unitCost) || unitCost < 0) {
          setAdjustmentError("Unit cost must be zero or greater.");
          return;
        }
        payload.unitCost = unitCost;
      }
    }

    setAdjusting(true);

    try {
      const result = await adjustStock(payload);
      setAdjustmentSuccess(
        `${result.message} ${selectedProduct.name}: ${result.previousStock} → ${result.newStock} units.`
      );
      setAdjustment({
        ...EMPTY_ADJUSTMENT,
        productId: adjustment.productId,
      });
      await loadInventory();
    } catch (err) {
      setAdjustmentError(
        err.message || "Could not record the stock adjustment. Please try again."
      );
    } finally {
      setAdjusting(false);
    }
  }

  const filteredProducts = useMemo(() => {
    const term = search.trim().toLowerCase();
    return products.filter((product) => {
      const matchesSearch =
        !term ||
        String(product.name || "").toLowerCase().includes(term) ||
        String(product.sku || "").toLowerCase().includes(term);
      const matchesCategory =
        categoryFilter === "all" ||
        product.categoryId === categoryFilter;
      return matchesSearch && matchesCategory;
    });
  }, [products, search, categoryFilter]);

  const lowStock = products.filter(
    (p) => Number(p.currentStock) > 0 &&
      Number(p.currentStock) <= Number(p.minimumStockLevel)
  ).length;

  const outOfStock = products.filter(
    (p) => Number(p.currentStock) <= 0
  ).length;

  const stockValue = products.reduce(
    (sum, p) => sum + (Number(p.currentStock) || 0) *
      (Number(p.buyingPrice) || 0),
    0
  );

  const selectedAdjustmentProduct = products.find(
    (product) => product.id === adjustment.productId
  );

  return (
    <div className="inventory-page">
      <div className="inventory-heading">
        <div>
          <span className="section-kicker">PRODUCT MANAGEMENT</span>
          <h1>Inventory</h1>
          <p>Track products, stock levels, and inventory value.</p>
        </div>
        <div className="inventory-heading-actions">
          <button
            type="button"
            className="inventory-add-button"
            onClick={() => {
              setShowAdjustment((current) => !current);
              setAdjustmentError("");
              setAdjustmentSuccess("");
            }}
          >
            {showAdjustment ? "Close Adjustments" : "Adjust Stock"}
          </button>
          <button
            type="button"
            className="inventory-add-button"
            onClick={() => {
              setShowForm((current) => !current);
              setFormError("");
              setSuccessMessage("");
            }}
          >
            {showForm ? "Cancel" : "+ Add Product"}
          </button>
          <button
            type="button"
            className="inventory-refresh"
            onClick={loadInventory}
            disabled={loading}
          >
            {loading ? "Loading..." : "↻ Refresh"}
          </button>
        </div>
      </div>

      {successMessage && (
        <div className="inventory-success" role="status">
          {successMessage}
        </div>
      )}

      {adjustmentSuccess && (
        <div className="inventory-success" role="status">
          {adjustmentSuccess}
        </div>
      )}

      {showAdjustment && (
        <section className="inventory-form-card">
          <div className="inventory-form-heading">
            <div>
              <span className="section-kicker">STOCK CONTROL</span>
              <h2>Stock adjustments</h2>
              <p>Record incoming stock, damaged items, or correct the balance to match a physical count.</p>
            </div>
          </div>

          {adjustmentError && (
            <div className="inventory-error" role="alert">
              <p>{adjustmentError}</p>
            </div>
          )}

          {products.filter((product) => product.isActive).length === 0 ? (
            <div className="inventory-error">
              <p>No active products are available for stock adjustment.</p>
            </div>
          ) : (
            <form className="inventory-form" onSubmit={handleStockAdjustment}>
              <label>
                Product *
                <select
                  name="productId"
                  value={adjustment.productId}
                  onChange={updateAdjustment}
                  required
                >
                  <option value="">Select a product</option>
                  {products
                    .filter((product) => product.isActive)
                    .map((product) => (
                      <option key={product.id} value={product.id}>
                        {product.name} — Stock: {product.currentStock}
                      </option>
                    ))}
                </select>
                {selectedAdjustmentProduct && (
                  <small>
                    Current stock: {selectedAdjustmentProduct.currentStock} units
                  </small>
                )}
              </label>

              <label>
                Adjustment type *
                <select
                  name="action"
                  value={adjustment.action}
                  onChange={updateAdjustment}
                  required
                >
                  <option value="Receive">Receive stock</option>
                  <option value="Damage">Record damaged stock</option>
                  <option value="Correction">Correct physical count</option>
                </select>
              </label>

              {adjustment.action === "Correction" ? (
                <label>
                  Actual physical count *
                  <input
                    type="number"
                    name="physicalCount"
                    value={adjustment.physicalCount}
                    onChange={updateAdjustment}
                    min="0"
                    step="1"
                    required
                    placeholder="e.g. 10"
                  />
                  <small>Enter the total quantity you physically counted.</small>
                </label>
              ) : (
                <label>
                  Quantity *
                  <input
                    type="number"
                    name="quantity"
                    value={adjustment.quantity}
                    onChange={updateAdjustment}
                    min="1"
                    step="1"
                    required
                  />
                </label>
              )}

              {adjustment.action === "Receive" && (
                <label>
                  Unit cost (KSh)
                  <input
                    type="number"
                    name="unitCost"
                    value={adjustment.unitCost}
                    onChange={updateAdjustment}
                    min="0"
                    step="0.01"
                    placeholder={
                      selectedAdjustmentProduct
                        ? `Default: ${selectedAdjustmentProduct.buyingPrice}`
                        : "Uses product buying price"
                    }
                  />
                  <small>Leave blank to use the product's buying price.</small>
                </label>
              )}

              <label>
                Reference number
                <input
                  name="referenceNumber"
                  value={adjustment.referenceNumber}
                  onChange={updateAdjustment}
                  maxLength={100}
                  placeholder="e.g. GRN-2026-001"
                />
              </label>

              <label>
                Notes
                <input
                  name="notes"
                  value={adjustment.notes}
                  onChange={updateAdjustment}
                  maxLength={500}
                  placeholder="Reason for the adjustment"
                />
              </label>

              {selectedAdjustmentProduct && (
                <div className="inventory-adjustment-preview">
                  <strong>Selected stock</strong>
                  <span>{selectedAdjustmentProduct.name}</span>
                  <span>Available: {selectedAdjustmentProduct.currentStock} units</span>
                  {adjustment.action === "Correction" &&
                    adjustment.physicalCount !== "" &&
                    Number.isInteger(Number(adjustment.physicalCount)) &&
                    Number(adjustment.physicalCount) >= 0 && (
                      <span>
                        Difference: {Number(adjustment.physicalCount) - Number(selectedAdjustmentProduct.currentStock)} units
                      </span>
                    )}
                </div>
              )}

              <div className="inventory-form-actions">
                <button
                  type="button"
                  className="inventory-cancel-button"
                  onClick={() => {
                    setShowAdjustment(false);
                    setAdjustmentError("");
                    setAdjustmentSuccess("");
                  }}
                  disabled={adjusting}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="inventory-add-button"
                  disabled={adjusting || loading}
                >
                  {adjusting ? "Recording..." : "Record adjustment"}
                </button>
              </div>
            </form>
          )}
        </section>
      )}

      {showForm && (
        <section className="inventory-form-card">
          <div className="inventory-form-heading">
            <div>
              <span className="section-kicker">NEW ITEM</span>
              <h2>Add a product</h2>
              <p>Enter the product details and any stock you already have.</p>
            </div>
          </div>

          {formError && (
            <div className="inventory-error" role="alert">
              <p>{formError}</p>
            </div>
          )}

          {categories.length === 0 ? (
            <div className="inventory-error">
              <p>No categories are available. Create a category before adding a product.</p>
            </div>
          ) : (
            <form className="inventory-form" onSubmit={handleCreateProduct}>
              <label>
                Product name *
                <input
                  name="name"
                  value={form.name}
                  onChange={updateForm}
                  placeholder="e.g. HP EliteBook 840 G5"
                  maxLength={200}
                  required
                />
              </label>

              <label>
                SKU / product code *
                <input
                  name="sku"
                  value={form.sku}
                  onChange={updateForm}
                  placeholder="e.g. HP-840-G5"
                  maxLength={100}
                  required
                />
              </label>

              <label>
                Category *
                <select
                  name="categoryId"
                  value={form.categoryId}
                  onChange={updateForm}
                  required
                >
                  <option value="">Select a category</option>
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </label>

              <label>
                Description
                <input
                  name="description"
                  value={form.description}
                  onChange={updateForm}
                  placeholder="Optional product description"
                />
              </label>

              <label>
                Buying price (KSh) *
                <input
                  type="number"
                  name="buyingPrice"
                  value={form.buyingPrice}
                  onChange={updateForm}
                  min="0"
                  step="0.01"
                  required
                />
              </label>

              <label>
                Selling price (KSh) *
                <input
                  type="number"
                  name="sellingPrice"
                  value={form.sellingPrice}
                  onChange={updateForm}
                  min="0"
                  step="0.01"
                  required
                />
              </label>

              <label>
                Minimum stock level *
                <input
                  type="number"
                  name="minimumStockLevel"
                  value={form.minimumStockLevel}
                  onChange={updateForm}
                  min="0"
                  step="1"
                  required
                />
              </label>

              <label>
                Opening stock quantity
                <input
                  type="number"
                  name="openingStock"
                  value={form.openingStock}
                  onChange={updateForm}
                  min="0"
                  step="1"
                />
                <small>Enter 0 if you have no stock yet.</small>
              </label>

              <div className="inventory-form-actions">
                <button
                  type="button"
                  className="inventory-cancel-button"
                  onClick={() => {
                    setShowForm(false);
                    setFormError("");
                    setForm(EMPTY_FORM);
                  }}
                  disabled={saving}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="inventory-add-button"
                  disabled={saving || categories.length === 0}
                >
                  {saving ? "Saving product..." : "Save product"}
                </button>
              </div>
            </form>
          )}
        </section>
      )}

      <div className="inventory-stats">
        <article className="inventory-stat">
          <span>Total products</span>
          <strong>{products.length}</strong>
          <small>Products in your catalogue</small>
        </article>
        <article className="inventory-stat">
          <span>Low stock</span>
          <strong>{lowStock}</strong>
          <small>Products at or below minimum</small>
        </article>
        <article className="inventory-stat">
          <span>Out of stock</span>
          <strong>{outOfStock}</strong>
          <small>Products needing replenishment</small>
        </article>
        <article className="inventory-stat">
          <span>Stock value</span>
          <strong>{formatMoney(stockValue)}</strong>
          <small>Estimated cost of current stock</small>
        </article>
      </div>

      <section className="inventory-table-card">
        <div className="inventory-table-heading">
          <div>
            <h2>Product catalogue</h2>
            <p>Search and filter your registered products.</p>
          </div>
          <span className="inventory-count">{filteredProducts.length} products</span>
        </div>

        <div className="inventory-filters">
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search product name or SKU..."
            aria-label="Search products"
          />
          <select
            value={categoryFilter}
            onChange={(event) => setCategoryFilter(event.target.value)}
            aria-label="Filter by category"
          >
            <option value="all">All categories</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </div>

        {error && (
          <div className="inventory-error">
            <p>{error}</p>
            <button onClick={loadInventory}>Try again</button>
          </div>
        )}

        {loading ? (
          <p className="inventory-message">Loading your inventory...</p>
        ) : !error && filteredProducts.length === 0 ? (
          <p className="inventory-message">No products match your search.</p>
        ) : !error ? (
          <div className="inventory-table-wrap">
            <table className="inventory-table">
              <thead>
                <tr>
                  <th>Product</th>
                  <th>SKU</th>
                  <th>Category</th>
                  <th>Buying price</th>
                  <th>Selling price</th>
                  <th>Current stock</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {filteredProducts.map((product) => {
                  const stock = Number(product.currentStock) || 0;
                  const minimum = Number(product.minimumStockLevel) || 0;
                  const status =
                    stock <= 0
                      ? "Out of stock"
                      : stock <= minimum
                      ? "Low stock"
                      : "In stock";
                  const category = categories.find(
                    (item) => item.id === product.categoryId
                  );

                  return (
                    <tr key={product.id}>
                      <td>
                        <strong>{product.name}</strong>
                        {product.description && <small>{product.description}</small>}
                      </td>
                      <td>{product.sku || "—"}</td>
                      <td>{category?.name || "—"}</td>
                      <td>{formatMoney(product.buyingPrice)}</td>
                      <td>{formatMoney(product.sellingPrice)}</td>
                      <td>{stock}</td>
                      <td>
                        <span className={`inventory-status ${status.toLowerCase().replaceAll(" ", "-")}`}>
                          {status}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : null}
      </section>
    </div>
  );
}
