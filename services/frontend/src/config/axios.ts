import axios from 'axios';

// Configure axios defaults
axios.defaults.baseURL = import.meta.env.VITE_API_BASE_URL;
console.log(axios.defaults.baseURL);
export default axios;
