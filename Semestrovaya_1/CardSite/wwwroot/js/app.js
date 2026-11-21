(() => {
  "use strict";

  const $ = (sel, root = document) => root.querySelector(sel);
  const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];
  const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;

  const ESC = { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" };
  const esc = v => String(v ?? "").replace(/[&<>"']/g, c => ESC[c]);
  const safeUrl = u => /^\s*(javascript|vbscript|data):/i.test(u || "") ? "" : esc(u);
  const num = v => (v == null || v === "") ? "—" : (Number.isFinite(+v) ? (+v).toLocaleString("en-US") : esc(v));

  const menuToggle = $("#menuToggle");
  const mainNav = $("#mainNav");
  menuToggle?.addEventListener("click", () => {
    const open = mainNav.classList.toggle("open");
    menuToggle.setAttribute("aria-expanded", String(open));
  });

  $(".card-search")?.addEventListener("click", e => {
    e.preventDefault();
    $("#search")?.scrollIntoView({ behavior: reducedMotion ? "auto" : "smooth", block: "start" });
    $("#keyword")?.focus({ preventScroll: true });
  });

  const form = $("#searchForm");
  const toggle = $("#advancedToggle");
  const panel = $("#advancedFilters");
  const badge = $(".filter-count");

  const multiFilters = ["type", "color", "rarity"];

  if (form) {
    const p = new URLSearchParams(location.search);

    const keyword = $("#keyword");
    if (keyword && p.has("q")) {
      keyword.value = p.get("q");
    }

    const series = $("#series");
    if (series && p.get("product")) {
      series.value = p.get("product");
    }

    multiFilters.forEach(name => {
      const selectedValues = p.getAll(name);

      $$(`input[name="${name}"]`, form).forEach(box => {
        box.checked = selectedValues.includes(box.value);
      });
    });

    const traitValues = p.getAll("specialTraits");

    $$('input[name="specialTraits"]', form).forEach(box => {
      box.checked = traitValues.includes(box.value);
    });
  }
  const fmtRange = n => Number(n).toLocaleString("en-US");
  const ranges = ["cost", "power", "comboPower"].map(name => {
    const min = $(`#${name}Min`), max = $(`#${name}Max`);
    const minOut = $(`#${name}MinValue`), maxOut = $(`#${name}MaxValue`);
    if (!min || !max) return null;

    const lo = +min.min, hi = +min.max, span = (hi - lo) || 1;
    const track = min.parentElement;
    const urlParams = new URLSearchParams(location.search);
    const initial = (el, fallback) => {
      let n = parseFloat(urlParams.get(el.name));
      if (!Number.isFinite(n)) n = parseFloat(el.getAttribute("value"));
      return Number.isFinite(n) ? Math.min(hi, Math.max(lo, n)) : fallback;
    };
    min.value = initial(min, lo);
    max.value = initial(max, hi);

    const sync = changed => {
      let a = +min.value, b = +max.value;
      if (a > b) {
        if (changed === min) a = b; else b = a;
        min.value = a; max.value = b;
      }
      if (minOut) minOut.textContent = fmtRange(a);
      if (maxOut) maxOut.textContent = fmtRange(b);
      track.style.setProperty("--f", (a - lo) / span);
      track.style.setProperty("--t", (b - lo) / span);
      min.classList.toggle("on-top", (a - lo) / span > 0.5);
    };
    min.addEventListener("input", () => sync(min));
    max.addEventListener("input", () => sync(max));
    sync();

    return { isDefault: () => +min.value === lo && +max.value === hi };
  }).filter(Boolean);

  const allTraits = $("#allSpecialTraits");
  const traitBoxes = $$('input[name="specialTraits"]').filter(i => i !== allTraits);
  if (allTraits) {
    allTraits.checked = !traitBoxes.some(b => b.checked);
    allTraits.addEventListener("change", () => {
      if (allTraits.checked) traitBoxes.forEach(b => (b.checked = false));
      else if (!traitBoxes.some(b => b.checked)) allTraits.checked = true;
    });
    traitBoxes.forEach(b => b.addEventListener("change", () => {
      allTraits.checked = !traitBoxes.some(x => x.checked);
    }));
  }

  let params = new URLSearchParams(location.search);
  const flat = s => s.trim().replace(/\s+/g, " ");
  const labelOf = (name, value) => {
    const input = $(`input[name="${name}"][value="${CSS.escape(value)}"]`, form);
    return flat(input?.closest("label")?.textContent || value);
  };
  const numParam = (key, fallback) => {
    const n = parseFloat(params.get(key));
    return Number.isFinite(n) ? n : fallback;
  };

  function appliedFilters() {
    const list = [];
    const add = (label, text, drop, advanced) => list.push({ label, text, drop, advanced });

    const q = (params.get("q") || "").trim();
    if (q) add("keyword", q, p => p.delete("q"), false);

    const product = params.get("product");
    if (product) {
      const opt = $(`#series option[value="${CSS.escape(product)}"]`);
      add("series", opt ? flat(opt.textContent) : product, p => p.delete("product"), false);
    }

    [["type", "type"], ["color", "color"], ["rarity", "rarity"]].forEach(([name, label]) => {
      const values = params.getAll(name);

      values.forEach(value => {
        add(
            label,
            labelOf(name, value),
            p => {
              const remaining = p.getAll(name).filter(v => v !== value);

              p.delete(name);

              remaining.forEach(v => p.append(name, v));
            },
            true
        );
      });
    });

    [["cost", "cost"], ["power", "power"], ["comboPower", "combo power"]].forEach(([name, label]) => {
      const minEl = $(`#${name}Min`), maxEl = $(`#${name}Max`);
      if (!minEl || !maxEl) return;
      const lo = +minEl.min, hi = +minEl.max;
      const a = numParam(`${name}Min`, lo), b = numParam(`${name}Max`, hi);
      if (a !== lo || b !== hi) {
        add(label, `${fmtRange(a)}–${fmtRange(b)}`, p => { p.delete(`${name}Min`); p.delete(`${name}Max`); }, true);
      }
    });

    params.getAll("specialTraits").filter(v => v && v !== "all").forEach(v =>
        add("trait", labelOf("specialTraits", v), p => {
          const rest = p.getAll("specialTraits").filter(x => x !== v);
          p.delete("specialTraits");
          rest.forEach(x => p.append("specialTraits", x));
        }, true));

    return list;
  }

  function renderActiveFilters() {
    params = new URLSearchParams(location.search);

    const applied = form ? appliedFilters() : [];

    if (activeBox) {
      activeBox.innerHTML = applied.map((f, i) => `
            <span class="active-filter">
                <span>${esc(f.label)}: ${esc(f.text)}</span>
                <button type="button"
                        data-remove="${i}"
                        aria-label="Remove filter ${esc(f.label)}: ${esc(f.text)}">
                    &times;
                </button>
            </span>
        `).join("");
    }

    const advancedCount = applied.filter(f => f.advanced).length;

    if (badge) {
      badge.hidden = advancedCount === 0;
      badge.textContent = advancedCount;
    }

    return advancedCount;
  }

  const activeBox = $("#activeFilters");
  if (activeBox) {
    activeBox.addEventListener("click", async e => {
      const btn = e.target.closest("[data-remove]");

      if (!btn) return;

      const applied = appliedFilters();
      const filter = applied[Number(btn.dataset.remove)];

      if (!filter) return;

      const next = new URLSearchParams(location.search);

      filter.drop(next);

      next.set("page", "1");

      await loadCards(next);
      renderActiveFilters();
    });
  }

  renderActiveFilters();

  function setOpen(open, animate = true) {
    if (!panel || !toggle) return;
    if (!animate) panel.classList.add("no-transition");
    panel.classList.toggle("is-open", open);
    panel.inert = !open;
    toggle.setAttribute("aria-expanded", String(open));
    if (!animate) { void panel.offsetHeight; panel.classList.remove("no-transition"); }
  }

  if (form && toggle && panel) {
    toggle.addEventListener("click", () => setOpen(toggle.getAttribute("aria-expanded") !== "true"));
    setOpen(renderActiveFilters() > 0, false); 
  }

  const grid = $("#cardGrid");
  if (grid) {
    if (!$(".card-tile", grid)) {
      grid.insertAdjacentHTML("afterend",
          `<div class="empty-state"><p>No cards match these filters. Try a different keyword or remove a filter.</p></div>`);
    }

    const pagination = $("#pagination");

    function buildPageUrl(searchParams, page) {
      const params = new URLSearchParams(searchParams);
      params.set("page", String(page));

      return `/cards?${params.toString()}`;
    }

    function renderPagination(data, searchParams) {
      if (!pagination) return;

      if (data.totalPages <= 1) {
        pagination.hidden = true;
        return;
      }

      pagination.hidden = false;

      const previousPage = Math.max(1, data.currentPage - 1);
      const nextPage = Math.min(data.totalPages, data.currentPage + 1);

      const previousClass = data.hasPrevious ? "active" : "disabled";
      const nextClass = data.hasNext ? "active" : "disabled";

      let html = `
        <a class="page-arrow ${previousClass}"
           href="${esc(buildPageUrl(searchParams, previousPage))}"
           aria-label="Previous page">
          &#10094;
        </a>
      `;

      for (let page = 1; page <= data.totalPages; page++) {
        const currentClass = page === data.currentPage ? "active" : "";

        html += `
          <a class="page-number ${currentClass}"
             href="${esc(buildPageUrl(searchParams, page))}">
            ${page}
          </a>
        `;
      }

      html += `
        <a class="page-arrow ${nextClass}"
           href="${esc(buildPageUrl(searchParams, nextPage))}"
           aria-label="Next page">
          &#10095;
        </a>
      `;

      pagination.innerHTML = html;
    }

    function renderCards(cards) {
      grid.innerHTML = cards.map(card => `
        <a href="/cards/${encodeURIComponent(card.id)}" class="card-tile">
          <img
            src="${safeUrl(card.imageUrl)}"
            alt="${esc(card.cardNumber)} ${esc(card.name)}"
            loading="lazy">
        </a>
      `).join("");
    }

    async function loadCards(searchParams, updateHistory = true) {
      const params = new URLSearchParams(searchParams);

      if (!params.has("page")) {
        params.set("page", "1");
      }

      grid.setAttribute("aria-busy", "true");

      try {
        const response = await fetch(`/api/cards?${params.toString()}`, {
          method: "GET",
          headers: {
            Accept: "application/json"
          }
        });

        if (!response.ok) {
          throw new Error(`Server returned ${response.status}`);
        }

        const data = await response.json();

        if (updateHistory) {
          history.pushState({}, "", `/cards?${params.toString()}`);
        }

        renderCards(data.cards);

        const resultCount = $("#resultCount");

        if (resultCount) {
          resultCount.textContent = data.totalCards;
        }

        renderPagination(data, params);

        let emptyState = grid.nextElementSibling;

        if (data.cards.length === 0) {
          if (!emptyState?.classList.contains("empty-state")) {
            emptyState = document.createElement("div");
            emptyState.className = "empty-state";
            grid.after(emptyState);
          }

          emptyState.innerHTML = `
            <p>No cards match these filters. Try a different keyword or remove a filter.</p>
          `;
        } else if (emptyState?.classList.contains("empty-state")) {
          emptyState.remove();
        }

      } catch (error) {
        console.error("AJAX catalog loading failed:", error);
        alert("Could not load cards. Please try again.");
      } finally {
        grid.removeAttribute("aria-busy");
      }
    }

    const clearSearch = $(".clear-search");

    if (clearSearch && form) {
      clearSearch.addEventListener("click", async e => {
        e.preventDefault();

        const keyword = $("#keyword");
        if (keyword) keyword.value = "";

        const series = $("#series");

        if (series) {
          const fb11Option = [...series.options].find(option =>
              option.textContent.includes("[FB11]")
          );

          if (fb11Option) {
            series.value = fb11Option.value;
          }
        }

        $$('input[name="type"], input[name="color"], input[name="rarity"]', form)
            .forEach(box => box.checked = false);

        traitBoxes.forEach(box => box.checked = false);

        if (allTraits) {
          allTraits.checked = true;
        }

        ranges.forEach(range => {
          const name = range === ranges[0] ? "cost" :
              range === ranges[1] ? "power" : "comboPower";

          const min = $(`#${name}Min`);
          const max = $(`#${name}Max`);

          if (!min || !max) return;

          min.value = min.min;
          max.value = max.max;

          min.dispatchEvent(new Event("input", { bubbles: true }));
          max.dispatchEvent(new Event("input", { bubbles: true }));
        });

        await loadCards(new URLSearchParams());

        renderActiveFilters();
      });
    }

    if (form) {
      form.addEventListener("submit", async e => {
        e.preventDefault();

        const searchParams = new URLSearchParams(new FormData(form));

        searchParams.set("page", "1");

        const searchButton = $('button[type="submit"]', form);

        if (searchButton) {
          searchButton.disabled = true;
        }

        try {
          await loadCards(searchParams);
        } finally {
          if (searchButton) {
            searchButton.disabled = false;
          }
        }
      });
    }

    if (pagination) {
      pagination.addEventListener("click", async e => {
        const link = e.target.closest("a");

        if (!link || !pagination.contains(link)) return;

        if (
            e.button !== 0 ||
            e.ctrlKey ||
            e.metaKey ||
            e.shiftKey ||
            e.altKey
        ) {
          return;
        }

        e.preventDefault();

        if (link.classList.contains("disabled")) return;

        const url = new URL(link.href, location.origin);
        const searchParams = new URLSearchParams(url.search);

        await loadCards(searchParams);
      });
    }

    const markBroken = img => {
      const tile = img.closest(".card-tile");
      if (!tile) return;
      tile.classList.add("is-broken");
      tile.dataset.label = img.alt || "Image unavailable";
      img.hidden = true;
    };
    grid.addEventListener("error", e => { if (e.target.tagName === "IMG") markBroken(e.target); }, true);
    $$("img", grid).forEach(img => { if (img.complete && img.naturalWidth === 0 && img.src) markBroken(img); });
  }

  const CARD_RE = /^\/cards\/(\d+)\/?$/;
  const baseTitle = document.title;
  const ACCENTS = { red: "#e92844", blue: "#248de0", green: "#1da66b", yellow: "#ffdb33", black: "#a58bff" };
  const colorKey = c => Object.keys(ACCENTS).find(k => String(c || "").toLowerCase().includes(k)) || "";

  let modal = null, lastFocus = null, controller = null;

  function ensureModal() {
    if (modal) return modal;
    modal = document.createElement("div");
    modal.id = "cardModal";
    modal.className = "card-modal";
    modal.inert = true;
    modal.setAttribute("role", "dialog");
    modal.setAttribute("aria-modal", "true");
    modal.setAttribute("aria-label", "Card details");
    document.body.append(modal);

    modal.addEventListener("click", e => {
      if (e.target === modal || e.target.closest(".card-modal-close")) return closeCardDetails();
      const retry = e.target.closest("[data-retry]");
      if (retry) openCardDetails(retry.dataset.retry, false);
    });
    return modal;
  }

  function show(inner, accent = "") {
    const m = ensureModal();
    const wasOpen = m.classList.contains("is-open");
    m.innerHTML = `
      <div class="card-modal-content"${accent ? ` style="--accent:${accent}"` : ""}>
        <button class="card-modal-close" type="button" aria-label="Close">&times;</button>
        ${inner}
      </div>`;
    if (!wasOpen) {
      lastFocus = document.activeElement;
      document.body.style.overflow = "hidden";
      m.inert = false;
      void m.offsetWidth;
      m.classList.add("is-open");
    }
    $(".card-modal-close", m).focus({ preventScroll: true });
  }

  function hide() {
    controller?.abort();
    document.title = baseTitle;
    if (!modal || !modal.classList.contains("is-open")) return;
    modal.classList.remove("is-open");
    modal.inert = true;
    document.body.style.overflow = "";
    lastFocus?.focus?.({ preventScroll: true });
  }

  const loadingHtml = `
    <div class="card-details" aria-busy="true">
      <div class="card-details-image"><div class="skeleton" style="aspect-ratio:63/88"></div></div>
      <div class="card-details-info">
        <div class="skeleton" style="height:16px;width:28%"></div>
        <div class="skeleton" style="height:38px;width:70%;margin:14px 0 26px"></div>
        <div class="skeleton" style="height:220px"></div>
      </div>
    </div>`;

  async function openCardDetails(id, push = true) {
    controller?.abort();
    controller = new AbortController();
    const { signal } = controller;

    if (push) history.pushState({ cardModal: true }, "", `/cards/${id}${location.search}`);
    show(loadingHtml);

    try {
      const res = await fetch(`/api/cards/${encodeURIComponent(id)}`, { signal, headers: { Accept: "application/json" } });
      if (!res.ok) throw new Error(`Server returned ${res.status}`);
      renderCard(await res.json());
    } catch (err) {
      if (err.name === "AbortError") return;
      console.error("Could not load card details:", err);
      show(`
        <div class="modal-message">
          <h2>Couldn’t load this card</h2>
          <p>Check your connection and try again.</p>
          <button class="retry" type="button" data-retry="${esc(id)}">Try again</button>
        </div>`);
    }
  }

  function renderCard(card) {
    const key = colorKey(card.color);

    const stats = [
      ["Card type", esc(card.cardType || "—")],
      ["Color", `${key ? `<i class="color ${key}"></i> ` : ""}${esc(card.color || "—")}`],
      ["Cost", num(card.cost)],
      ["Specified cost", esc(card.specifiedCost || "—")],
      ["Power", num(card.power)],
      ["Combo power", num(card.comboPower)]
    ].map(([label, value]) => `
        <div class="stat">
            <div class="stat__label">${label}</div>
            <div class="stat__value">${value}</div>
        </div>
    `).join("");

    const traits = card.specialTraits?.length
        ? card.specialTraits
            .map(t => `<span class="trait">${esc(t)}</span>`)
            .join("")
        : "—";

    const products = card.products?.length
        ? card.products.map(p => {
          const img = safeUrl(p.imageUrl);
          const url = safeUrl(p.productUrl);

          return `
                <div class="product-item">
                    ${img
              ? `<img class="product-item__image" src="${img}" alt="${esc(p.name)}" loading="lazy">`
              : ""
          }

                    <span class="product-item__name">${esc(p.name)}</span>

                    ${url
              ? `<a class="btn-pill" href="${url}" target="_blank" rel="noopener noreferrer">View product</a>`
              : ""
          }
                </div>
            `;
        }).join("")
        : "—";

    show(`
        <div class="card-detail">

            <div class="card-detail__img">
                <img
                    src="${safeUrl(card.imageUrl)}"
                    alt="${esc(card.name)}"
                >
            </div>

            <div class="card-detail__info">

                <div class="card-head">
                    <span class="card-head__number">
                        ${esc(card.cardNumber)}
                    </span>

                    <span class="badge">
                        ${esc(card.rarity || "—")}
                    </span>
                </div>

                <h2 class="card-head__name">
                    ${esc(card.name)}
                </h2>

                <div class="stats">
                    ${stats}
                </div>

                <section class="info-panel">
                    <h3 class="info-panel__title">Special Traits</h3>
                    <div class="info-panel__value">
                        ${traits}
                    </div>
                </section>

                <section class="info-panel">
                    <h3 class="info-panel__title">Skills</h3>
                    <div class="info-panel__value">
                        ${esc(card.skills || "—")}
                    </div>
                </section>

                <section class="info-panel">
                    <h3 class="info-panel__title">Where to get it</h3>
                    <div class="info-panel__value">
                        ${esc(card.set || "—")}${card.setCode ? ` [${esc(card.setCode)}]` : ""}
                    </div>
                </section>

                <section class="info-panel">
                    <h3 class="info-panel__title">Products</h3>
                    <div class="info-panel__value info-panel__value--row">
                        ${products}
                    </div>
                </section>

            </div>
        </div>
    `, ACCENTS[key] || "");

    document.title = `${card.name} | ${baseTitle}`;
  }

  function closeCardDetails() {
    if (history.state?.cardModal) {
      history.back();
    } else {
      history.pushState({}, "", `/cards${location.search}`);
      hide();
    }
  }

  window.addEventListener("popstate", () => {
    const m = location.pathname.match(CARD_RE);

    if (m) {
      openCardDetails(m[1], false);
      return;
    }

    hide();

    if (location.pathname === "/cards" || location.pathname === "/") {
      loadCards(new URLSearchParams(location.search), false);
    }
  });

  document.addEventListener("click", e => {
    if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
    const tile = e.target.closest(".card-tile");
    const m = tile?.getAttribute("href")?.match(CARD_RE);
    if (!m) return;
    e.preventDefault();
    openCardDetails(m[1]);
  });

  document.addEventListener("keydown", e => {
    if (!modal?.classList.contains("is-open")) return;
    if (e.key === "Escape") return closeCardDetails();
    if (e.key !== "Tab") return;
    const items = $$('a[href], button:not([disabled])', modal);
    if (!items.length) return;
    const first = items[0], last = items[items.length - 1];
    if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
    else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
  });

  const initial = location.pathname.match(CARD_RE);
  if (initial && grid) openCardDetails(initial[1], false);
})();