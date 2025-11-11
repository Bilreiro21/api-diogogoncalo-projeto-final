// Footer year
document
  .querySelectorAll("#year")
  .forEach((el) => (el.textContent = new Date().getFullYear()));

// LocalStorage cart
const CART_KEY = "dsx_cart";
function getCart() {
  try {
    return JSON.parse(localStorage.getItem(CART_KEY)) || [];
  } catch {
    return [];
  }
}
function setCart(items) {
  localStorage.setItem(CART_KEY, JSON.stringify(items));
  renderCart();
}

// Add to cart buttons
document.querySelectorAll("[data-add-to-cart]").forEach((btn) => {
  btn.addEventListener("click", () => {
    const id = btn.getAttribute("data-id");
    const name = btn.getAttribute("data-name");
    const price = parseFloat(btn.getAttribute("data-price"));
    const cart = getCart();
    const existing = cart.find((i) => i.id === id);
    if (existing) existing.qtd += 1;
    else cart.push({ id, name, price, qtd: 1 });
    setCart(cart);
    btn.classList.add("btn-success");
    btn.textContent = "Added";
    setTimeout(() => {
      btn.classList.remove("btn-success");
      btn.textContent = "Add to cart";
    }, 900);
  });
});

// Render cart table (on Pages/cart.html)
function renderCart() {
  const body = document.getElementById("cart-body");
  if (!body) return;
  const cart = getCart();
  body.innerHTML = "";
  let subtotal = 0;

  cart.forEach((item) => {
    const total = item.price * item.qtd;
    subtotal += total;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${item.name}</td>
      <td>€${item.price.toFixed(2)}</td>
      <td>
        <div class="input-group input-group-sm" style="max-width:120px">
          <button class="btn btn-outline-secondary" data-dec>-</button>
          <input class="form-control text-center" value="${item.qtd}" />
          <button class="btn btn-outline-secondary" data-inc>+</button>
        </div>
      </td>
      <td>€${total.toFixed(2)}</td>
      <td><button class="btn btn-sm btn-outline-danger" data-remove>Remove</button></td>
    `;

    tr.querySelector("[data-dec]").addEventListener("click", () => {
      item.qtd = Math.max(1, item.qtd - 1);
      setCart(cart);
    });
    tr.querySelector("[data-inc]").addEventListener("click", () => {
      item.qtd += 1;
      setCart(cart);
    });
    tr.querySelector("[data-remove]").addEventListener("click", () => {
      const idx = cart.findIndex((i) => i.id === item.id);
      cart.splice(idx, 1);
      setCart(cart);
    });

    body.appendChild(tr);
  });

  const sub = document.getElementById("cart-subtotal");
  if (sub) sub.textContent = `€${subtotal.toFixed(2)}`;
}
renderCart();
