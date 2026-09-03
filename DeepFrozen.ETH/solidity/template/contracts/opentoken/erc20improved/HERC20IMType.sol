// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

library HERC20IMType {

    struct constructParam {
        string name;
        string symbol;
        uint256 cap;
        bool supportTransfer;
        bool supportMint;
        bool supportBurn;
    }

}
