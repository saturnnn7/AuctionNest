/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: ['class', '.dark-theme'],
  theme: {
    extend: {
      colors: {
        emerald: { 600: '#059669' },
        slate:   { 900: '#0f172a', 50: '#f8fafc' },
        rose:    { 600: '#e11d48' },
        amber:   { 500: '#f59e0b' },
      },
      animation: {
        'pulse-slow': 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'pulse-fast': 'pulse 0.8s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      }
    }
  },
  plugins: []
};
