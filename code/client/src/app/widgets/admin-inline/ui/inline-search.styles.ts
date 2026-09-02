export const INLINE_SEARCH_STYLES = `
  .inline-panel,
  .inline-panel table,
  .inline-panel td {
    color: var(--ink);
  }
  .inline-panel th {
    color: var(--ink-secondary);
  }
  .inline-panel td {
    vertical-align: top;
  }
  .audit-filters {
    display: flex;
    gap: 0.625rem;
    align-items: center;
    margin-bottom: 1rem;
    flex-wrap: wrap;
  }
  .audit-search-wrap {
    position: relative;
    flex: 1 1 200px;
    min-width: 180px;
    max-width: 22rem;
  }
  .audit-search-wrap .search-icon {
    position: absolute;
    left: 1rem;
    top: 50%;
    transform: translateY(-50%);
    color: var(--ink-muted);
    font-size: 0.85rem;
    pointer-events: none;
  }
  .admin-search {
    width: 100%;
    height: 42px;
    padding: 0 2.25rem 0 2.5rem;
    border: 1px solid var(--border);
    border-radius: var(--radius-full);
    font-size: 0.875rem;
    font-weight: 500;
    font-family: var(--font);
    color: var(--ink);
    background: rgba(255, 255, 255, 0.9);
    box-sizing: border-box;
    transition:
      border-color var(--t-fast),
      box-shadow var(--t-fast);
  }
  .admin-search::placeholder {
    color: #757575;
    font-weight: 500;
  }
  .admin-search:focus {
    outline: none;
    border-color: var(--interactive-text);
    box-shadow: 0 0 0 3px rgba(0, 69, 123, 0.12);
  }
  .clear-search {
    position: absolute;
    right: 0.6rem;
    top: 50%;
    transform: translateY(-50%);
    display: flex;
    align-items: center;
    justify-content: center;
    width: 1.4rem;
    height: 1.4rem;
    border: none;
    background: none;
    cursor: pointer;
    padding: 0;
    border-radius: 50%;
    color: var(--ink-muted);
    font-size: 0.75rem;
    transition:
      color var(--t-fast),
      background-color var(--t-fast),
      transform var(--t-fast);
  }
  @media (hover: hover) and (pointer: fine) {
    .clear-search:hover {
      color: var(--ink);
      background: var(--interactive-subtle);
    }
  }
  .clear-search:active {
    transform: translateY(-50%) scale(0.92);
  }
  .equipment-identifiers,
  .equipment-location {
    display: grid;
    gap: 0.2rem;
    min-width: 10rem;
    line-height: 1.35;
  }
  .equipment-identifiers strong {
    color: var(--ink);
    font-size: 0.7rem;
  }
  .equipment-identifiers span,
  .equipment-location span {
    color: var(--ink-secondary);
  }
  .equipment-location span:not(:first-child) {
    color: var(--ink-muted);
    font-size: 0.75rem;
  }

  @media (max-width: 768px) {
    .audit-filters {
      flex-direction: column;
      align-items: stretch;
    }
    .audit-search-wrap,
    .admin-search {
      width: 100%;
      max-width: none;
      min-width: 0;
      flex: 0 0 auto;
    }
    .admin-search {
      height: 40px;
      font-size: 16px;
    }
  }
`;
