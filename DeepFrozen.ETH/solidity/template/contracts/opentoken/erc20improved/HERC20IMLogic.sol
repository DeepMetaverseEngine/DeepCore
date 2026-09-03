// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC20IMLayout.sol";
import "../../nameserver/ownable/OwnableLogic.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "../../nameserver/reentrancy/ReentrancyLogic.sol";
import "../erc20/HERC20Logic.sol";
import "./HERC20IMInterface.sol";

import "../../nameserver/nameService/NameServiceInterface.sol";
import "../assetVault/AssetVaultInterface.sol";

abstract contract HERC20IMLogic is HERC20IMLayout, OwnableLogic, NameServiceRefLogic, ReentrancyLogic, HERC20Logic, HERC20IMInterface {
    using SafeMath for uint256;
    using EnumerableSet for EnumerableSet.AddressSet;

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoMint(){
        _sudoMint = true;
        _;
        _sudoMint = false;
    }

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoBurn(){
        _sudoBurn = true;
        _;
        _sudoBurn = false;
    }

    //for non-sudo mint, the "to" must be the msg.sender
    function _mintNormal(uint256 amount) internal onlyOnce {
        _mint(msg.sender, amount);
    }

    function _mintSudo(address to, uint256 amount) internal enableSudoMint onlyOnce {
        _mint(to, amount);
    }

    //for non-sudo burn, the "from" must be the msg.sender
    function _burnNormal(uint256 amount) internal onlyOnce {
        _burn(msg.sender, amount);
    }

    function _burnSudo(address from, uint256 amount) internal enableSudoBurn onlyOnce {
        _burn(from, amount);
    }

    //=====

    function setAccessControl(address accessControl_) override external onlyOwner {
        _setAccessControl(accessControl_);
        emit SetAccessControl(accessControl_);
    }

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) override external onlyOwner {
        _supportTransfer = supportTransfer_;
        _supportMint = supportMint_;
        _supportBurn = supportBurn_;
        emit SetSupport(supportTransfer_, supportMint_, supportBurn_);
    }

    function setBlockListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _blockListFrom[from[i]] = flag;
        }
    }

    function setBlockListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _blockListTo[to[i]] = flag;
        }
    }

    function setPrivilegeListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _privilegeListFrom[from[i]] = flag;
        }
    }

    function setPrivilegeListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _privilegeListTo[to[i]] = flag;
        }
    }
    //==================================

    function support() override view external returns (bool supportTransfer, bool supportMint, bool supportBurn){
        return (_supportTransfer, _supportMint, _supportBurn);
    }

    function transferTxs() override view external returns (uint256) {
        return _transferTxs;
    }

    function transferAmounts() override view external returns (uint256) {
        return _transferAmounts;
    }

    function interactAccountsLength() override view external returns (uint256){
        return _interactAccounts.length();
    }

    function interactAccountsContains(address who) override view external returns (bool){
        return _interactAccounts.contains(who);
    }

    function interactedAccountsAt(uint256 index) override view external returns (address){
        return _interactAccounts.at(index);
    }

    //privilege overrides block and supportTransfer
    function _beforeTokenTransfer(address from, address to, uint256 amount) virtual override(HERC20Logic) internal {

        NameServiceInterface ns = nsUnsafe();

        if (from == address(0) && to != address(0)) {
            //mint mode
            //some how you call _mint :) , you must have done job of mint permission
            _beforeTokenTransferMint(to, amount, address(ns));

        }
        else if (from != address(0) && to == address(0)) {
            //burn mode
            //some how you call _burn :) , you must have done job of burn permission
            _beforeTokenTransferBurn(from, amount, address(ns));

        } else if (from != address(0) && to != address(0)) {
            //transfer
            _beforeTokenTransferTransfer(from, to, amount, address(ns));

        } else {
            revert("transfer, 'from' and 'to' should never be address(0) at same time");
        }

        //impossible to hit the 2^256 :)

    unchecked{_transferTxs = _transferTxs + 1;}
    unchecked{_transferAmounts = _transferAmounts + amount;}

        if (from != address(0)) {
            _interactAccounts.add(from);
        }
        if (to != address(0)) {
            _interactAccounts.add(to);
        }

        HERC20Logic._beforeTokenTransfer(from, to, amount);

    }

    function _beforeTokenTransferMint(address to, uint256, address ns) view internal {

        //not support mint
        if (_sudoMint) {
            //pass
        } else {
            if (ns != address(0) && NameServiceInterface(ns).isPrivileged(to)) {
                //privilege pass
            } else if (_privilegeListTo[to]) {
                //privilege pass
            } else if (ns != address(0) && NameServiceInterface(ns).isBlocked(to)) {
                revert("ac.isBlocked-to");
            } else if (_blockListTo[to]) {
                revert("blockListTo");
            } else {
                if (_supportMint) {

                } else {
                    revert("un-support mint");
                }
            }
        }
    }

    function _beforeTokenTransferBurn(address from, uint256, address ns) view internal {

        if (_sudoBurn) {
            //pass
        } else {
            if (ns != address(0) && NameServiceInterface(ns).isPrivileged(from)) {
                //privilege overrides blockList
            } else if (_privilegeListFrom[from]) {
                //privilege overrides blockList
            } else if (ns != address(0) && NameServiceInterface(ns).isBlocked(from)) {
                revert("ac.isBlocked-from");
            } else if (_blockListFrom[from]) {
                revert("blockListFrom");
            } else {
                if (_supportBurn) {

                } else {
                    revert("un-support burn");
                }
            }
        }
    }

    function _beforeTokenTransferTransfer(address from, address to, uint256, address ns) view internal {

        if (ns != address(0) &&
            (NameServiceInterface(ns).isPrivileged(from) || NameServiceInterface(ns).isPrivileged(to))
        ) {
            //privilege overrides blockList
        } else if (_privilegeListFrom[from] || _privilegeListTo[to]) {
            //privilege overrides blockList
        } else if (ns != address(0) &&
            (NameServiceInterface(ns).isBlocked(from) || NameServiceInterface(ns).isBlocked(to))
        ) {
            revert("ac.isBlocked-from||ac.isBlocked-to");
        } else if (_blockListFrom[from] || _blockListTo[to]) {
            revert("blockListFrom||blockListTo");
        } else {
            if (_supportTransfer) {

            } else {
                revert("un-support transfer");
            }
        }
    }

}
