import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import {createBrowserRouter, RouterProvider} from "react-router";
import './index.css';
import App from "./App.tsx";
import HomePage from "@/HomePage.tsx";
import User from "@/User.tsx";


const router = createBrowserRouter([
    {
        path: "/",
        element: <App/>,
        children: [
            {
                path: "/",
                element: <HomePage/>
            },
            {
                path: "/:userId",
                element: <User/>
            }
        ]
    },
]);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
      <RouterProvider router={router} />
  </StrictMode>,
)
